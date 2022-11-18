using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Automation.Peers;
using System.Windows.Ink;

namespace HeavyTracks
{
    /// <summary>
    /// handles any interfacing with the Spotify WEB API.
    /// </summary>
    public static class SpotifyContext
    {
        public static void initialize(string client_id, string? user_token = null)
        {
            m_client_id = client_id;
            m_user_token = user_token;
        }

        public delegate void MissingCreds();

        public static MissingCreds? missing_creds;

        public static int MaxWeight
        {
            get => max_weight;
            set
            {
                if (value < min_weight)
                    throw new ArgumentException("max weight must be greater than min weight");

                max_weight = value;
            }
        }

        public static int MaxWeight
        {
            get => max_weight;
            set
            {
                if (value < min_weight)
                    throw new ArgumentException("max weight must be greater than min weight");

                max_weight = value;
            }
        }

        /// <summary>
        /// retrieves the id of the current user.
        /// if the token is missing or expired, this function returns null.
        /// </summary>
        public static string? getUserId()
        {
            // get the user id from the web api, if it is not already cached.

            if(m_user_id == null)
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

        public static List<Playlist> getPlaylists()
        {
            var items = getAllItems(HttpMethod.Get, $"users/{getUserId()}/playlists");
            List<Playlist> playlists = new();

            foreach(var item in items)
            {
                Playlist playlist = new(item["collaborative"]?.Value<bool>() ?? false, item["public"]?.Value<bool>() ?? false, 
                    item["name"]?.ToString() ?? "INVALID", item["description"]?.ToString() ?? "", item["id"]?.ToString() ?? "");

                playlists.Add(playlist);
            }

            return playlists;
        }

        public static void fillPlaylist(ref Playlist target)
        {
            var items = getAllItems(HttpMethod.Get, $"playlists/{target.id}/tracks");

            foreach(var item in items)
            {
                var track_item = item["track"];
                string id = track_item["id"]?.ToString() ?? "";

                var existing_track = target.tracks.Find(item => item.id == id);

                if (existing_track != null)
                    existing_track.weight++;
                else
                {
                    Trace.WriteLine(item);
                    Track new_track = new(0, track_item["name"]?.ToString() ?? "NOT FOUND", "NOT YET IMPLEMENTED", track_item["duration_ms"]?.Value<int>() ?? -1, id);
                    target.tracks.Append(new_track);
                }

            }

        }

        public static void createPlaylist(Playlist source, string name, bool overwrite_if_similar)
        {
            // start by deleting all of the current contents of the playlist stored on spotify.

            Playlist current_playlist = source.copy();

            fillPlaylist(ref current_playlist);

            int batch = 0;

            while(batch * MAX_URIS < current_playlist.tracks.Count)


        }

        /// <summary>
        /// retrieves a spotify user token, by allowing the user to log in to their spotify account.
        /// if login fails, or the user cancels, the token is not updated.
        /// </summary>
        public static void newUserToken()
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
            System.Diagnostics.Process.Start("explorer", $"\"{builder}\"");

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

        private static string m_client_id = "";
        private static string? m_user_token;
        private static string? m_user_id = null;

        private static HttpClient m_client = new();

        private static readonly Uri AUTH_ENDPOINT = new("https://accounts.spotify.com/authorize");
        private static readonly Uri API_ENDPOINT = new("https://api.spotify.com/v1/");
        private static readonly uint PORT = 8888;
        private static readonly string SCOPE = "playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";
        private static readonly int MAX_LIMIT = 50;
        private static readonly int MAX_URIS = 100;
        private static readonly int MAX_OFFSET = 100_000;

        private static int max_weight = 5;
        private static int min_weight = -5;

        /// <summary>
        /// constructs a HttpRequestMessage with the specified method.
        /// its uri will point to API_ENDPOINT/endpoint, and will be loaded with the necessarry authorization.
        /// </summary>
        /// <param name="method"> the http request method </param>
        /// <param name="endpoint"> what uri should be suffixed to the api endpoint </param>
        /// <returns> the HttpRequestMethod, null if authorization token is missing </returns>
        private static HttpRequestMessage? spotifyApiReq(HttpMethod method, string endpoint)
        {
            if (m_user_token == null)
            {
                missing_creds?.Invoke();

                return null;
            }

            HttpRequestMessage req_msg = new(method, $"{API_ENDPOINT}{endpoint}");

            req_msg.Headers.Add("Authorization", $"Bearer {m_user_token}");

            return req_msg;
        }

        /// <summary>
        /// sends the passed request, and returns the response.
        /// if authorization fails, or the request object is null, this method returns null.
        /// </summary>
        /// <param name="msg"> request message to be sent </param>
        private static HttpResponseMessage? sendApiReq(HttpRequestMessage? msg)
        {
            if (msg == null)
                return null;

            var response = m_client.SendAsync(msg).Result;

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                missing_creds?.Invoke();

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
        private static List<JObject> getAllItems(HttpMethod method, string endpoint, string query = "")
        {
            /// a spotify query with a list type of result, will only return a maximum of 50 entries per request.
            /// in order to recieve all of the values in for example a playlist,
            /// get requests will be sent, with an increasing offset of 50, will be sent, until all playlist tracks have been retrieved.
            /// if this function fails at any point, an empty list will be returned.

            List<JObject> items = new();

            int pages = 0;

            while (pages * MAX_LIMIT < MAX_OFFSET)
            {
                var msg = spotifyApiReq(method, endpoint);
                
                if (msg == null)
                    break;
                
                var builder = new UriBuilder(msg.RequestUri ?? new(""));
                var item_query = HttpUtility.ParseQueryString(query);

                item_query["limit"] = MAX_LIMIT.ToString();
                item_query["offset"] = (MAX_LIMIT * pages).ToString();
                builder.Query = item_query.ToString();

                msg.RequestUri = builder.Uri;


                var response = sendApiReq(msg);

                if(response == null || !response.IsSuccessStatusCode) break;

                var content = JObject.Parse(response.Content.ReadAsStringAsync().Result);

                var recieved_items = content["items"]?.ToArray();

                foreach (var item in recieved_items ?? Enumerable.Empty<JToken>())
                    items.Add((JObject)item);

                if (content["total"]?.Value<int>() - pages * MAX_LIMIT < MAX_LIMIT)
                    break;

                pages++;
            }

            return items;
        }
    }

    public class Track
    {
        public Track(int _weight, string _name, string _artist, int _length, string _id)
        {
            weight = _weight;
            name = _name;
            artist = _artist;
            length = _length;
            id = _id;
        }

        public Track copy()
        {
            return new(weight, name, artist, length, id);
        }

        public int weight;
        public string name;
        public string artist;
        public string id;
        public int length;
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

        int trackCount()
        {
            int count = 0;

            foreach(Track track in tracks)
                count += track.weight

        }

        public bool is_collaborative;
        public bool is_public;
        public string name;
        public string description;
        public string id;

        public List<Track> tracks = new();
    }
}
