using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Automation.Peers;
using System.Windows.Ink;
using System.Windows.Interop;

namespace HeavyTracks.Models
{
    /// <summary>
    /// handles any interfacing with the Spotify WEB API.
    /// </summary>
    public class SpotifyWeigher
    {
        public SpotifyWeigher(string client_id, string? user_token = null)
        {
            m_client_id = client_id;
            m_user_token = user_token;
        }

        public delegate void ApiErrCallback(HttpStatusCode status, JObject content);

        public ApiErrCallback? missing_creds;
        public ApiErrCallback? api_error;

        public int MaxWeight
        {
            get => max_weight;
            set
            {
                max_weight = value;
            }
        }


        /// <summary>
        /// retrieves the id of the current user.
        /// if the token is missing or expired, this function returns null.
        /// </summary>
        public string? getUserId()
        {
            // get the user id from the web api, if it is not already cached.

            if (m_user_id == null)
            {
                var request = spotifyApiReq(HttpMethod.Get, "me");

                if (request != null)
                {
                    var response = sendApiReq(request);

                    if (response?.IsSuccessStatusCode ?? false)
                        m_user_id = JObject.Parse(response.Content.ReadAsStringAsync().Result)["id"]?.ToString();
                }

            }

            return m_user_id;
        }

        /// <summary>
        /// 
        /// checks whether a token is currently avaliable in the SpotifyContext.
        /// does not check if the token is expired.
        /// 
        /// </summary>
        public bool hasToken() => m_user_token != null;

        /// <summary>
        /// retrieves a list of all the playlists that has been created by the active user.
        /// 
        /// the order of the playlists should match the order of the displayed in Spotify.
        /// 
        /// </summary>
        public List<Playlist> getPlaylists()
        {
            var items = getAllItems(HttpMethod.Get, $"users/{getUserId()}/playlists");
            List<Playlist> playlists = new();

            foreach (var item in items)
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
        public string? getPlaylistImgUrl(Playlist playlist)
        {
            var req = spotifyApiReq(HttpMethod.Get, $"playlists/{playlist.id}/images");
            var res = sendApiReq(req);

            var content = JToken.Parse(res?.Content.ReadAsStringAsync().Result ?? "[]");

            if(content.Count() > 0)
                return content[0]?["url"]?.ToString();
            else
                return null;
        }

        public List<Track> getPlaylistTracks(Playlist target)
        {
            Dictionary<string, (Track, uint)> unordered_tracks = new();

            var items = getAllItems(HttpMethod.Get, $"playlists/{target.id}/tracks");

            uint i = 0;

            foreach (var item in items)
            {
                var track_item = item["track"];
                string id = track_item?["id"]?.ToString() ?? "";

                if (unordered_tracks.ContainsKey(id))
                    unordered_tracks[id].Item1.weight++;
                else
                {
                    Track new_track = new(1, track_item?["name"]?.ToString() ?? "NOT FOUND", "NOT YET IMPLEMENTED", track_item?["duration_ms"]?.Value<int>() ?? -1, track_item?["is_local"]?.Value<bool>() ?? false, id, track_item?["uri"]?.ToString() ?? "");
                    unordered_tracks[id] = (new_track, i++);
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
        public Playlist? createPlaylist(string name, bool? is_public = null, bool? is_collab = null, string? description = null, bool reference_similar = false)
        {

            if (reference_similar)
            {
                // check if a similar playlist exists.

                var existing_playlists = getPlaylists();

                foreach (Playlist playlist in existing_playlists)
                {
                    // if all of this is true, a similar playlist has been found.
                    if (name == playlist.name &&
                        (is_public ?? playlist.is_public) == playlist.is_public &&
                        (is_collab ?? playlist.is_collaborative) == playlist.is_collaborative &&
                        (description ?? playlist.description) == playlist.description)
                        return playlist;
                }
            }

            var create_req = spotifyApiReq(HttpMethod.Post, $"users/{getUserId()}/playlists");

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

            var response_content = JObject.Parse(response?.Content.ReadAsStringAsync().Result ?? "{}");

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
        public void pushTracks(List<Track> tracks, Playlist playlist, bool overwrite)
        {
            if (overwrite)
            {
                // start by removing all existing tracks (except for local ones) from the playlist.

                List<Track> tracks_to_remove = getPlaylistTracks(playlist).FindAll(track => !track.is_local);

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
        /// retrieves a spotify user token, by allowing the user to log in to their spotify account.
        /// if login fails, or the user cancels, the token is not updated.
        /// </summary>
        public void newUserToken()
        {
            // create the authentication url.
            var builder = new UriBuilder(AUTH_ENDPOINT);

            builder.Port = -1;

            var q = HttpUtility.ParseQueryString(builder.Query);

            q["response_type"] = "token";
            q["client_id"] = m_client_id;
            q["scope"] = SCOPE;
            q["redirect_uri"] = $"http://localhost:{PORT}/callback";


            builder.Query = q.ToString();

            // open the constructed authorization url in the default browser.
            Process.Start("explorer", $"\"{builder}\"");

            // prepare the local webserver, to retreive the authentication token.
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{PORT}/callback/");
            listener.Start();

            // if the QueryString does not contain an access_token parameter, it either means no access_token was recieved,
            // or that the token is stored in the url hash
            // as the hash is not sent to the webserver, we instead return a html file containing a small javascript snippet,
            // that converts the hash parameters to standard url parameters, that can be read by the webserver.

            while (true)
            {
                var context = listener.GetContext();


                var req = context.Request;
                var res = context.Response;

                if (req.QueryString["access_token"] == null)
                {
                    res.StatusCode = 200;
                    res.ContentType = "text/html";

                    var content = File.ReadAllBytes("InsertHash.html");

                    res.OutputStream.Write(content, 0, content.Count());
                    res.Close();
                }
                else
                {
                    m_user_token = req.QueryString["access_token"]!;

                    res.StatusCode = 200;
                    res.ContentType = "text/html";

                    // html file, that auto closes the tab, is sent as a response.
                    var content = File.ReadAllBytes("ClosePage.html");

                    res.OutputStream.Write(content, 0, content.Count());
                    res.Close();

                    break;
                }
            }
        }

        private string m_client_id = "";
        private string? m_user_token;
        private string? m_user_id;

        private HttpClient m_client = new();

        private static readonly Uri AUTH_ENDPOINT = new("https://accounts.spotify.com/authorize");
        private static readonly Uri API_ENDPOINT = new("https://api.spotify.com/v1/");
        private static readonly uint PORT = 8888;
        private static readonly string SCOPE = "playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";
        private static readonly int MAX_RECV = 50;
        private static readonly int MAX_SEND = 100;
        private static readonly int MAX_OFFSET = 100_000;

        private int max_weight = 5;

        /// <summary>
        /// constructs a HttpRequestMessage with the specified method.
        /// its uri will point to API_ENDPOINT/endpoint, and will be loaded with the necessarry authorization header values.
        /// </summary>
        /// <param name="method"> the http request method </param>
        /// <param name="endpoint"> what uri should be suffixed to the api endpoint </param>
        /// <returns> the HttpRequestMethod, null if authorization token is missing </returns>
        private HttpRequestMessage? spotifyApiReq(HttpMethod method, string endpoint)
        {
            HttpRequestMessage req_msg = new(method, $"{API_ENDPOINT}{endpoint}");

            req_msg.Headers.Add("Authorization", $"Bearer {m_user_token}");

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

            var response = m_client.SendAsync(msg).Result;

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                missing_creds?.Invoke(response.StatusCode, JObject.Parse(response.Content.ReadAsStringAsync().Result));

                return null;
            }
            else if (!response.IsSuccessStatusCode)
            {
                api_error?.Invoke(response.StatusCode, JObject.Parse(response.Content.ReadAsStringAsync().Result));

                return null;
            }

            return response;
        }

        /// <summary>
        /// retrieves all items of a spotify endpoint, which returns a variable number of items.
        /// </summary>
        /// <param name="method"> request method </param>
        /// <param name="endpoint"> spotify api endpoint, which returns a variable number of items</param>
        /// <param name="query"> additional query parameters </param>
        /// <returns> list of the varaible json items returned from the spotify api </returns>
        private List<JObject> getAllItems(HttpMethod method, string endpoint, string query = "")
        {
            /// a spotify query with a list type of result, will only return a maximum of 50 entries per request.
            /// in order to recieve all of the values in for example a playlist,
            /// get requests will be sent, with an increasing offset of 50, will be sent, until all playlist tracks have been retrieved.
            /// if this function fails at any point, an empty list will be returned.

            List<JObject> items = new();

            int pages = 0;

            while (pages * MAX_RECV < MAX_OFFSET)
            {
                var msg = spotifyApiReq(method, endpoint);

                if (msg == null)
                    break;

                var builder = new UriBuilder(msg.RequestUri ?? new(""));
                var item_query = HttpUtility.ParseQueryString(query);

                item_query["limit"] = MAX_RECV.ToString();
                item_query["offset"] = (MAX_RECV * pages).ToString();
                builder.Query = item_query.ToString();

                msg.RequestUri = builder.Uri;


                var response = sendApiReq(msg);

                if (response == null || !response.IsSuccessStatusCode) break;

                var content = JObject.Parse(response.Content.ReadAsStringAsync().Result);

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
        /// <param name="body"> (optional) the rest of the body that should be passed to the endpoint, should not contain [property name] </param>
        /// <param name="query"> (optional) additional query parameters to be passed to the endpoint </param>
        private bool sendAllItems(HttpMethod method, string endpoint, string property_name, List<JToken> values, JObject? body = null, string query = "")
        {
            body ??= new();

            int pages = 0;

            while (pages * MAX_SEND < values.Count())
            {
                var msg = spotifyApiReq(method, endpoint);

                if (msg == null)
                    break;

                // add the passed query parameters

                var builder = new UriBuilder(msg.RequestUri ?? new(""));
                var item_query = HttpUtility.ParseQueryString(query);

                builder.Query = item_query.ToString();

                msg.RequestUri = builder.Uri;

                // construct a json array, which contains at most MAX_SEND elements.

                var jarr = new JArray();

                for (int i = pages * MAX_SEND; i < (pages + 1) * MAX_SEND && i < values.Count(); i++)
                    jarr.Add(values[i]);

                body[property_name] = jarr;

                msg.Content = new StringContent(body.ToString());
                msg.Content.Headers.ContentType = new("application/json");

                var response = sendApiReq(msg);

                if (!response?.IsSuccessStatusCode ?? true)
                    return false;

                pages++;
            }

            return true;
        }
    }

    public class Track
    {
        public Track(int _weight, string _name, string _artist, int _length, bool _is_local, string _id, string _uri)
        {
            weight = _weight;
            name = _name;
            artist = _artist;
            length = _length;
            is_local = _is_local;
            id = _id;
            uri = _uri;
        }

        public Track copy()
        {
            return new(weight, name, artist, length, is_local, id, uri);
        }

        public bool is_local { get; set; }
        public int weight { get; set; }
        public string name { get; set; }
        public string artist { get; set; }
        public string id { get; set; }
        public string uri { get; set; }
        public int length { get; set; }
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
