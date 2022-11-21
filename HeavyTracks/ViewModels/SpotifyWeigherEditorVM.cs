using HeavyTracks.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeavyTracks.ViewModels
{
    public class SpotifyWeigherEditorVM: BaseViewModel
    {
        public SpotifyWeigher weigher;

        public List<Playlist> playlists { get; set; } = new();

        public Playlist? active_playlist;
        public List<Track>? tracks { get; set; }

        public SpotifyWeigherEditorVM(SpotifyWeigher _weigher)
        {
            weigher = _weigher;

            weigher.newUserToken();

            updatePlaylists();

            foreach (Playlist playlist in playlists)
            {
                Trace.WriteLine(playlist.name);
            }
        }

        public void updatePlaylists()
        {
            playlists = weigher.getPlaylists();
            onPropertyChanged(nameof(playlists));
        }

        public void selectPlaylist(int indx)
        {
            active_playlist = playlists[indx];
            tracks = weigher.getPlaylistTracks(active_playlist);

            onPropertyChanged(nameof(tracks));
            onPropertyChanged(nameof(active_playlist));
        }



    }
}
