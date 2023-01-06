using DynamicData;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Tomlyn;
using Tomlyn.Model;

namespace HeavyTracks.Models
{
    public static class TomlynExtension
    {
        public static T get<T>(this TomlTable table, string key) => (T)table[key];

        public static TomlTable get(this TomlTable table, string key) => get<TomlTable>(table, key);

        public static void set<T>(this TomlTable table, string key, T new_value) => table[key] = new_value!;
        
        public static void set(this TomlTable table, string key, TomlTable new_value) => table.set<TomlTable>(key, new_value);
    }

    /// <summary>
    /// handles any interfacing with the Spotify WEB API.
    /// </summary>
    public class SpotifyWeigher
    {
        public SpotifyWeigher(string client_id = "")
        {
            m_client_id = client_id;
        }

        public delegate void ApiErrCallback(HttpStatusCode status, JObject content);

        /// <summary>
        /// retrieves the id of the current user.
        /// if the token is missing or expired, this function returns null.
        /// </summary>
        public async Task<string?> getUserId()
        {
            // get the user id from the web api, if it is not already cached.
            
            if (m_user_id == "")
            {
                var request = await spotifyApiReq(HttpMethod.Get, "me");

                if (request != null)
                {
                    var response = sendApiReq(request);

                    if (response?.IsSuccessStatusCode ?? false)
                        m_user_id = JObject.Parse(await response.Content.ReadAsStringAsync())["id"]!.ToString();
                }

            }

            return m_user_id;
        }

        public async Task<string?> getUserName()
        {
            var request = await spotifyApiReq(HttpMethod.Get, "me");

            if(request != null)
            {
                var response = sendApiReq(request);

                if (response?.IsSuccessStatusCode ?? false)
                    return JObject.Parse(await response.Content.ReadAsStringAsync())["display_name"]!.ToString();
            }

            return null;
        }

        /// <summary>
        /// retrieves a list of all the playlists that has been created by the active user.
        /// 
        /// the order of the playlists should match the order of the displayed in Spotify.
        /// 
        /// </summary>
        public async Task<List<Playlist>> getPlaylists()
        {
            var items = getAllItems(HttpMethod.Get, $"users/{await getUserId()}/playlists");
            List<Playlist> playlists = new();

            foreach (var item in await items)
            {
                Playlist playlist = new(item["collaborative"]?.Value<bool>() ?? false, item["public"]?.Value<bool>() ?? false,
                    item["name"]?.ToString() ?? "INVALID", item["description"]?.ToString() ?? "", item["id"]?.ToString() ?? "");

                playlists.Add(playlist);
            }

            return playlists;
        }
        
        /// <summary>
        /// 
        /// retrieve a url pointing to the passed playlists icon image.
        /// if no image is associated with the playlist, null is returned.
        /// 
        /// </summary>
        public async Task<string?> getPlaylistImgUrl(Playlist playlist)
        {
            var req = await spotifyApiReq(HttpMethod.Get, $"playlists/{playlist.id}/images");
            var res = sendApiReq(req);

            var content = JToken.Parse((await res?.Content.ReadAsStringAsync()) ?? "[]");

            if(content.Count() > 0)
                return content[0]?["url"]?.ToString();
            else
                return null;
        }

        public async Task<List<Track>> getPlaylistTracks(Playlist target)
        {
            Dictionary<string, (Track, uint)> unordered_tracks = new();

            var items = getAllItems(HttpMethod.Get, $"playlists/{target.id}/tracks");

            uint i = 0;
            int number = 0;

            foreach (var item in await items)
            {

                var track_item = item["track"];
                string uri = track_item?["uri"]?.ToString() ?? "";

                if (unordered_tracks.ContainsKey(uri))
                    unordered_tracks[uri].Item1.weight++;
                else
                {
                    number++;
                    Track new_track = new(1, track_item?["name"]?.ToString() ?? "NOT FOUND", track_item?["album"]?["name"]?.ToString() ?? "NOT FOUND", "NOT YET IMPLEMENTED", number, track_item?["duration_ms"]?.Value<int>() ?? -1, track_item?["is_local"]?.Value<bool>() ?? false, track_item?["id"]?.ToString() ?? "", uri);
                    unordered_tracks[uri] = (new_track, i++);
                }
            }

            var ordered_tracks = unordered_tracks.Values.ToList();

            ordered_tracks.OrderBy(i => i.Item2);

            List<Track> result = new(ordered_tracks.Count());

            foreach (var item in ordered_tracks)
                result.Add(item.Item1);

            return result;
        }

        /// <summary>
        /// 
        /// retrieve a url pointing to the passed tracks icon image.
        /// if no image is associated with the track, null is returned.
        /// 
        /// </summary>
        public string? getTrackImgUrl(Track track)
        {
            // TODO: should return the playlist cover image, where the track is from maybe?
            return null;
        }

        /// <summary>
        /// creates a playlist with the specified name, for the active user.
        /// 
        /// optionally returns a reference to already existing playlists,
        /// if the playlists are similar (same name, public status, collab status and description)
        /// 
        /// if more than one playlist are similar, the first one in the playlist list is referenced.
        /// 
        /// </summary>
        /// <param name="name"> name of the playlist to create </param>
        /// <param name="is_public"> specifies whether the playlist will be public or private </param>
        /// <param name="is_collab"> specifies whether the playlist will be a collaborative playlist </param>
        /// <param name="description"> the description of the new playlist. </param>
        /// <param name="reference_similar">
        ///     if true and a playlist with the same name already exists
        ///     and all other parameters either matches the playlist properties, or they are null:
        ///         return a playlist object which references this already existing playlist.
        ///     else:
        ///         create a new playlist with the specified name.
        ///             
        /// </param>
        /// <returns> the created / referenced playlist, or null if it failed </returns>
        public async Task<Playlist?> createPlaylist(string name, bool? is_public = null, bool? is_collab = null, string? description = null, bool reference_similar = false)
        {

            if (reference_similar)
            {
                // check if a similar playlist exists.

                var existing_playlists = getPlaylists();

                foreach (Playlist playlist in await existing_playlists)
                {
                    // if all of this is true, a similar playlist has been found.
                    if (name == playlist.name &&
                        (is_public ?? playlist.is_public) == playlist.is_public &&
                        (is_collab ?? playlist.is_collaborative) == playlist.is_collaborative &&
                        (description ?? playlist.description) == playlist.description)
                        return playlist;
                }
            }

            var create_req = await spotifyApiReq(HttpMethod.Post, $"users/{await getUserId()}/playlists");

            if (create_req == null)
                return null;

            JObject content = new();

            is_public ??= false;
            is_collab ??= false;
            description ??= "";

            content["name"] = name;
            content["public"] = is_public;
            content["collaborative"] = is_collab;
            content["description"] = description;

            create_req.Content = new StringContent(content.ToString());

            var response = sendApiReq(create_req);

            if (!response?.IsSuccessStatusCode ?? true)
                return null;

            var response_content = JObject.Parse((await response?.Content.ReadAsStringAsync()) ?? "{}");

            Playlist result = new((bool)is_collab, (bool)is_public, name, description, response_content["id"]?.ToString() ?? "");

            return result;
        }

        /// <summary>
        /// pushes the weighted tracks into the passed playlist.
        /// optionally overwrites the contents of the playlist,
        /// so any previous tracks before the push are deleted.
        /// 
        /// the playlist will contain a single track X times, where X is equal to the tracks weight.
        /// 
        /// local files will not be overwritten, even if overwrite is set to true.
        /// 
        /// </summary>
        /// <param name="tracks"> the weighted tracks to push to the playlist </param>
        /// <param name="playlist"> the playlist that will recieve the weighted tracks </param>
        /// <param name="overwrite"> wether the playlist should be overwritten or not, when pushed to </param>
        public async Task pushTracks(List<Track> tracks, Playlist playlist, bool overwrite)
        {
            if (overwrite)
            {
                // start by removing all existing tracks (except for local ones) from the playlist.

                List<Track> tracks_to_remove = (await getPlaylistTracks(playlist)).FindAll(track => !track.is_local);

                List<JToken> uris_to_remove = new(tracks_to_remove.Count());

                foreach (Track track in tracks_to_remove)
                    uris_to_remove.Add(JObject.Parse(@$"{{""uri"":""{track.uri}""}}"));

                sendAllItems(HttpMethod.Delete, $"playlists/{playlist.id}/tracks", "tracks", uris_to_remove);
            }

            List<JToken> uris_to_add = new();

            foreach (Track track in tracks)
            {
                // spotify web api does not yet support adding and removing local files.
                if (track.is_local)
                    continue;

                for (int i = 0; i < track.weight; i++)
                    uris_to_add.Add(new JValue(track.uri));
            }

            sendAllItems(HttpMethod.Post, $"playlists/{playlist.id}/tracks", "uris", uris_to_add);
        }

        /// <summary>
        /// 
        /// begins a "session", meaning any method calls, that require use of the spotify web api, can be used, after this method is called.
        /// 
        /// requires a client id to be loaded, before this is called (see loadClientId)
        /// 
        /// when called, the user is prompted for authentication, in order for subsequent calls to the spotify web api to be properly authenticated.
        /// if the user cancels this authentication, none of the methods that require the spotify web api, will work (see return value).
        /// 
        /// </summary>
        /// <returns> true if user consented to the session, false if user canceled the authentication </returns>
        public async Task<bool> beginSession()
        {
            // for setup of request, see: https://developer.spotify.com/documentation/general/guides/authorization/code-flow/
            
            // setup state and challenge code

            m_state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

            // neither the challenge code, or the challenge code hash can contain + or /, so these are replaced with '-' and '_'.
            // see: https://en.wikipedia.org/wiki/Base64#URL_applications

            m_challenge = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            Trace.WriteLine(m_challenge);
            m_challenge = m_challenge.Replace('+', '-').Replace('/', '_').TrimEnd('=');


            var vhash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(m_challenge)));
            vhash = vhash.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            
            Uri endpoint = genUri(AUTH_ENDPOINT, new(){
                { "client_id", m_client_id},
                { "response_type", "code" },
                { "redirect_uri",  $"http://localhost:{PORT}/callback"},
                { "state", m_state },
                { "scope", SCOPE },
                { "show_dialog", "true" },
                { "code_challenge_method", "S256" },
                { "code_challenge", vhash }
            });

            // open the constructed authorization url in the default browser.
            Process.Start(new ProcessStartInfo() { FileName = endpoint.ToString(), UseShellExecute = true });

            // prepare the local webserver, to show the authentication site to the user.
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{PORT}/callback/");
            listener.Start();

            // if the QueryString does not contain an access_token parameter, it either means no access_token was recieved,
            // or that the token is stored in the url hash
            // as the hash is not sent to the webserver, we instead return a html file containing a small javascript snippet,
            // that converts the hash parameters to standard url parameters, that can be read by the webserver.

            var context = listener.GetContext();


            var req = context.Request;
            var res = context.Response;
                    
            res.ContentType = "text/html";
                    
            // html file, that auto closes the tab, is sent as a response.
            var resp_content = File.ReadAllBytes("Assets/ClosePage.html");
            res.OutputStream.Write(resp_content, 0, resp_content.Count());

            // check if user canceled login
            if (req.QueryString["error"] != null)
                if (req.QueryString["error"] == "access_denied")
                    return false;
                else
                    throw new SpotifyWeigherException($"User authentication failed during consent dialog");
            
            string code = req.QueryString["code"]!;

            res.Close();
            
            // aquire access token and refresh token

            var access_response = sendHttpRequest(HttpMethod.Post, TOKEN_ENDPOINT,
                content_type: "application/x-www-form-urlencoded",
                body: await new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri",  $"http://localhost:{PORT}/callback" },
                    { "client_id", m_client_id },
                    { "code_verifier", m_challenge }
                }).ReadAsStringAsync());

            if (!access_response.IsSuccessStatusCode)
                throw new SpotifyWeigherException($"Access token get request failed", access_response);
            
            var access_body_json = JObject.Parse(await access_response.Content.ReadAsStringAsync());

            var access_params = getJsonParams(access_body_json, new() { "access_token", "refresh_token", "expires_in"});

            m_token = access_params["access_token"];
            m_refresh_token = access_params["refresh_token"];
            m_token_expires = DateTime.Now.AddSeconds(int.Parse(access_params["expires_in"]));

            m_user_id = "";

            return true;
        }

        /// <summary>
        /// attempts to load a client id from a cache file into memory.
        /// fails if file does not exists, or the cache file contains no client id.
        /// </summary>
        /// <param name="cache_file">
        /// toml file containing the key "client_id" in the global namespace, with the client id to be loaded, as its value-
        /// </param>
        /// <returns> true if load was succesfull, false if something went wrong. </returns>
        public bool loadClientId(string cache_file)
        {
            if (!File.Exists(cache_file))
                return false;
            
            TomlTable creds_file = Toml.ToModel(File.ReadAllText(cache_file));

            string tmp_id = creds_file.get<string>("client_id");

            if (tmp_id == "")
                return false;

            m_client_id = tmp_id;

            return true;
        }

        private string m_client_id = "";
        private string m_state = "";
        private string m_challenge = "";
        private string m_user_id = "";

        private string m_refresh_token = "";
        private string m_token = "";
        private DateTime m_token_expires;

        private HttpClient m_client = new();

        private static readonly Uri AUTH_ENDPOINT = new("https://accounts.spotify.com/authorize");
        private static readonly Uri API_ENDPOINT = new("https://api.spotify.com/v1/");
        private static readonly Uri TOKEN_ENDPOINT = new("https://accounts.spotify.com/api/token");
        private static readonly uint PORT = 8888;
        private static readonly string SCOPE = "playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";
        private static readonly int MAX_RECV = 50;
        private static readonly int MAX_SEND = 100;
        private static readonly int MAX_OFFSET = 100_000;

        /// <summary>
        /// sends a http request to the specified endpoint, with the specified content.
        /// </summary>
        /// <param name="method"> the http method to use when sending the request </param>
        /// <param name="endpoint"> where the request should be sent to </param>
        /// <param name="query_parameters"> what query parameters the request url should contain </param>
        /// <param name="headers"> what headers the request should contain </param>
        /// <param name="body"> what the body of the request should contain </param>
        /// <returns> the response for the sent http request </returns>
        private HttpResponseMessage sendHttpRequest(HttpMethod method, Uri endpoint, Dictionary<string, string>? headers = null, string? body=null, string? content_type=null)
        {
            HttpRequestMessage msg = new(method, endpoint);

            if (body != null)
                msg.Content = new StringContent(body, Encoding.UTF8, content_type);


            // setup headers
            if (headers != null)
                foreach (var header_param in headers)
                    msg.Headers.Add(header_param.Key, header_param.Value);

            HttpResponseMessage resp = m_client.Send(msg);

            return resp;
        }
        
        /// <summary>
        /// generates a URI with the passed query parameters dictionary,
        /// where a key will be seen as a parameter with a value equal to the keys corresponding value.
        /// </summary>
        private Uri genUri(Uri endpoint, Dictionary<string, string>? query_parameters = null)
        {
            var builder = new UriBuilder(endpoint);

            // setup query parameters
            if (query_parameters != null)
            {
                var q = HttpUtility.ParseQueryString(builder.Query);

                foreach (var param in query_parameters)
                    q[param.Key] = param.Value;

                builder.Query = q.ToString();
            }

            return builder.Uri;
        }

        /// <summary>
        /// does nothing if all the passed parameters exist in the json object, otherwise a [exception] exception is thrown, describing which parameter is missing.
        /// </summary>
        /// <param name="json"></param>
        /// <param name=""></param>
        /// <param name=""></param>
        private Dictionary<string, string> getJsonParams(JObject json, List<string> json_params)
        {
            Dictionary<string, string> res = new();

            foreach (var para in json_params)
                if (!json.ContainsKey(para))
                    throw new SpotifyWeigherException($"Missing [{para}] parameter in returned json body:\n{json}");
                else
                    res.Add(para, json[para]!.ToString()!);

            return res;
        }

        /// <summary>
        /// 
        /// retrieves a valid spotify api token, and auto refreshes if the currently stored token has expired.
        /// 
        /// </summary>
        /// <param name="force_refresh"> if true, forces a token refresh even if the current token has not expired</param>
        /// <returns> spotify api token </returns>
        /// <exception cref="SpotifyWeigherException"> gets thrown if refresh of token fails </exception>
        private async Task<string> getToken(bool force_refresh = false)
        {
            if(force_refresh || m_token_expires < DateTime.Now)
            {
                var resp = sendHttpRequest(HttpMethod.Post, TOKEN_ENDPOINT, body: await new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", m_refresh_token },
                    { "client_id", m_client_id },
                }).ReadAsStringAsync(), content_type: "application/x-www-form-urlencoded");

                if (!resp.IsSuccessStatusCode)
                    throw new SpotifyWeigherException("Refresh request failed", resp);

                var json_resp = JObject.Parse(await resp.Content.ReadAsStringAsync());

                var json_params = getJsonParams(json_resp, new() { "expires_in", "refresh_token", "token" });

                m_token_expires = DateTime.Now.AddSeconds(int.Parse(json_params["expires_in"]));
                m_refresh_token = json_params["refresh_token"];
                m_token = json_params["token"];
            }

            return m_token;
        }

        /// <summary>
        /// constructs a HttpRequestMessage with the specified method.
        /// its uri will point to API_ENDPOINT/endpoint, and will be loaded with the necessarry authorization header values.
        /// </summary>
        /// <param name="method"> the http request method </param>
        /// <param name="endpoint"> what uri should be suffixed to the api endpoint </param>
        /// <returns> the HttpRequestMethod, null if authorization token is missing </returns>
        private async Task<HttpRequestMessage?> spotifyApiReq(HttpMethod method, string endpoint)
        {
            HttpRequestMessage req_msg = new(method, $"{API_ENDPOINT}{endpoint}");

            req_msg.Headers.Add("Authorization", $"Bearer {await getToken()}");

            return req_msg;
        }

        /// <summary>
        /// sends the passed request, and returns the response.
        /// if authorization fails, or the request object is null, this method returns null.
        /// </summary>
        /// <param name="msg"> request message to be sent </param>
        private HttpResponseMessage? sendApiReq(HttpRequestMessage? msg)
        {
            if (msg == null)
                return null;

            var response = m_client.Send(msg);

            if (!response.IsSuccessStatusCode)
                throw new SpotifyWeigherException("Api request failed", response);

            return response;
        }

        /// <summary>
        /// retrieves all items of a spotify endpoint, which returns a variable number of items.
        /// </summary>
        /// <param name="method"> request method </param>
        /// <param name="endpoint"> spotify api endpoint, which returns a variable number of items</param>
        /// <param name="query"> additional query parameters </param>
        /// <returns> list of the varaible json items returned from the spotify api </returns>
        private async Task<List<JObject>> getAllItems(HttpMethod method, string endpoint)
        {
            /// a spotify query with a list type of result, will only return a maximum of 50 entries per request.
            /// in order to recieve all of the values in for example a playlist,
            /// get requests will be sent, with an increasing offset of 50, will be sent, until all playlist tracks have been retrieved.
            /// if this function fails at any point, an empty list will be returned.

            List<JObject> items = new();

            int pages = 0;

            while (pages * MAX_RECV < MAX_OFFSET)
            {
                var response = sendHttpRequest(method, genUri(new($"{API_ENDPOINT}{endpoint}"),
                    new(){
                    { "limit", MAX_RECV.ToString() },
                    { "offset", (MAX_RECV * pages).ToString() }
                }),
                headers: new() {
                    {"Authorization", $"Bearer {m_token}" }
                });

                if (!response.IsSuccessStatusCode)
                    throw new SpotifyWeigherException("Failed getting items", response);

                var content = JObject.Parse(await response.Content.ReadAsStringAsync());

                var recieved_items = content["items"]?.ToArray();

                foreach (var item in recieved_items ?? Enumerable.Empty<JToken>())
                    items.Add((JObject)item);

                if (content["total"]?.Value<int>() - pages * MAX_RECV < MAX_RECV)
                    break;

                pages++;
            }

            return items;
        }

        /// <summary>
        /// 
        /// Posts the passed values, under the passed property name,
        /// to an endpoint with a limited number of values pr. post.
        /// 
        /// </summary>
        /// <param name="method"> the http method of the http request, should probably be either POST, UPDATE or DELETE </param>
        /// <param name="endpoint"> endpoint to send the request to </param>
        /// <param name="property_name"> the name of the property that will contain the array of values </param>
        /// <param name="values"> the list of values that should be passed to the endpoint </param>
        /// <param name="body"> (optional) the rest of the body that should be passed to the endpoint, should not contain [property_name] </param>
        /// <param name="query"> (optional) additional query parameters to be passed to the endpoint </param>
        private void sendAllItems(HttpMethod method, string endpoint, string property_name, List<JToken> values, JObject? body = null)
        {
            body ??= new();

            int pages = 0;

            while (pages * MAX_SEND < values.Count())
            {
                // construct a json array, which contains at most MAX_SEND elements.

                var jarr = new JArray();

                for (int i = pages * MAX_SEND; i < (pages + 1) * MAX_SEND && i < values.Count(); i++)
                    jarr.Add(values[i]);

                body[property_name] = jarr;

                var response = sendHttpRequest(method, new($"{API_ENDPOINT}{endpoint}"),
                    headers: new() {
                    {"Authorization", $"Bearer {m_token}" }
                    },
                    body: body.ToString(), content_type: "application/json");

                if (!response?.IsSuccessStatusCode ?? true)
                    throw new SpotifyWeigherException("Sending of items failed", response!);

                pages++;
            }
        }
    }

    public class Track
    {
        public Track(int _weight, string _name, string _album, string _artist, int _number, int _length, bool _is_local, string _id, string _uri)
        {
            weight = _weight;
            name = _name;
            album = _album;
            artist = _artist;
            number = _number;
            length = _length;
            is_local = _is_local;
            id = _id;
            uri = _uri;
        }

        public Track copy()
        {
            return new(weight, name, album, artist, number, length, is_local, id, uri);
        }

        public bool is_local { get; set; }
        public int weight { get; set; }
        public string name { get; set; }
        public string album { get; set; }
        public string artist { get; set; }
        public int number { get; set; }
        public string id { get; set; }
        public string uri { get; set; }
        public int length { get; set; }
        public string length_str { get => string.Format("{0,2}:{1,2:00}", length / (1000 * 60), (length / 1000) % 60); }
    }

    public class Playlist
    {
        public Playlist(bool _is_collaborative, bool _is_public, string _name, string _description, string _id)
        {
            is_collaborative = _is_collaborative;
            is_public = _is_public;
            name = _name;
            description = _description;
            id = _id;
        }

        public Playlist copy()
        {
            Playlist playlist = new(is_collaborative, is_public, name, description, id);

            return playlist;
        }

        public bool is_collaborative { get; set; }
        public bool is_public { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string id { get; set; }
    }
}
