using HeavyTracks.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Diagnostics;
using System.Windows;
using static System.Formats.Asn1.AsnWriter;
using System.Web;
using System.Net;
using System.IO;
using System.Windows.Documents;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace HeavyTracks.Models
{
    public class PlaylistWeigher
    {
        
        public PlaylistWeigher(string client_id)
        {
            m_client_id = client_id;

            newUserToken();

            m_spotify = new SpotifyClient(m_user_token);
        }

        public async Task<List<FullPlaylist>> getPlaylists()
        {
            var playlists = m_spotify.Playlists.CurrentUsers();

            var plists = await m_spotify.PaginateAll(await playlists);

            List<FullPlaylist> plist = new();

            foreach (SimplePlaylist playlist in plists)
            {
                plist.Add((FullPlaylist)playlist);
            }

            return ;
        }

        public async Task<List<WeightedTrack>> getTracks(FullPlaylist playlist)
        {
            var tracks = await m_spotify.Playlists.GetItems(playlist.Id);
            Dictionary<string, WeightedTrack> weighted_tracks = new();

            uint i = 0;

            await foreach(var track in m_spotify.Paginate(tracks))
            {
                WeightedTrack new_track = new() { track = track.Track, Number = ++i };

                string? uri = getField<string>(new_track.track, "Uri");

                if (uri != null)
                    if (!weighted_tracks.ContainsKey(uri))
                        weighted_tracks[uri] = new_track;
                    else
                        weighted_tracks[uri].Weight++;
                else
                    Trace.WriteLine($"Warning: missing Uri on track! {track}");
            }

            List<WeightedTrack> ordered_weighted_tracks = weighted_tracks.Values.ToList();

            ordered_weighted_tracks.OrderBy(track => track.Number);

            return ordered_weighted_tracks;
        }

        public async Task pushTracks(List<WeightedTrack> tracks, FullPlaylist target_playlist, bool overwrite = true)
        {
            if (overwrite)
            {
                // clear playlist items
                List<PlaylistRemoveItemsRequest.Item> tracks_to_remove = new();

                await foreach (var playlist_track in m_spotify.Paginate(target_playlist.Tracks))
                {
                    tracks_to_remove.Add(new() { Uri = getField<string>(playlist_track.Track, "Uri") });
                }

                await m_spotify.Playlists.RemoveItems(target_playlist.Id, new() { Tracks = tracks_to_remove });
            }

            List<string> track_uris = new();

            foreach (WeightedTrack track in tracks)
                for (int i = 0; i < track.Weight; i++)
                    track_uris.Add(getField<string>(track.track, "Uri") ?? "");

            await m_spotify.Playlists.AddItems(target_playlist.Id, new(track_uris));

        }

        public async Task<FullPlaylist> createPlaylist(string name)
        {
            return await m_spotify.Playlists.Create((await m_spotify.UserProfile.Current()).Id, new(name));
        }

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
            System.Diagnostics.Process.Start("explorer", $"\"{builder}\"");

            // prepare the local webserver, to retreive the authentication token.
            using var listener = new HttpListener();
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
        
        public static T? getField<T>(IPlayableItem item, string field_name)
        {
            if(item is FullTrack track)
                return (T?) track.GetType().GetProperty(field_name)?.GetValue(track);
            else if(item is FullEpisode episode)
                return (T?) episode.GetType().GetProperty(field_name)?.GetValue(episode);

            return default;
        }

        protected string m_client_id;
        protected string m_user_token = "";
        protected SpotifyClient m_spotify;

        private static readonly Uri AUTH_ENDPOINT = new("https://accounts.spotify.com/authorize");
        private static readonly uint PORT = 8187;
        private static readonly string SCOPE = "playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";


    }

    public class WeightedTrack
    {
        public IPlayableItem track;

        public uint Weight
        {
            get => weight;
            set
            {
                if (value >= 1)
                    weight = value;
            }
        }

        public uint Number
        {
            get => number;
            set
            {
                if (value >= 1)
                    number = value;
            }
        }

        protected uint weight = 1;
        protected uint number = 1;
    }
}
