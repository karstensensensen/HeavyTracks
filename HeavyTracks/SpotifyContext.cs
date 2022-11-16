using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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

        public static void getPlaylists()
        {
            var request = spotifyApiReq(HttpMethod.Get, $"users/{getUserId()}/playlists");

            var response = sendApiReq(request);

            if(response?.IsSuccessStatusCode ?? false)
            {
                var obj = JObject.Parse(response.Content.ReadAsStringAsync().Result);

                Trace.WriteLine(obj);
            }

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
            System.Diagnostics.Process.Start("explorer", $"\"{builder.ToString()}\"");

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

        private static string m_client_id;
        private static string? m_user_token;
        private static string? m_user_id = null;

        private static HttpClient m_client = new();

        private static readonly Uri AUTH_ENDPOINT = new("https://accounts.spotify.com/authorize");
        private static readonly Uri API_ENDPOINT = new("https://api.spotify.com/v1/");
        private static readonly uint PORT = 8888;
        private static readonly string SCOPE = "playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";

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

            var response = m_client.Send(msg);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                missing_creds?.Invoke();

                return null;
            }

            return response;
        }
    }

    public class Track
    {
        public int weight;
        public string name;
        public string artist;
        public int length;
    }

    public class SpotifyTrack
    {
        public bool is_collaborative;
        public bool is_public;
        public string name;
        public string description;
        public string id;

        List<Track> tracks;
    }
}
