using HeavyTracks.Commands;
using HeavyTracks.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

            weigher.getPlaylistImgUrl(playlists[0]);
            weigher.getPlaylistImgUrl(playlists[1]);
            weigher.getPlaylistImgUrl(playlists[4]);

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

        public ICommand SelectPlaylistCmd { get; }
        
        public Playlist? ActivePlaylist
        { 
            get => active_playlist;
            set
            {
                active_playlist = value;

                if (active_playlist != null)
                {
                    tracks = weigher.getPlaylistTracks(active_playlist);

                    onPropertyChanged(nameof(tracks));
                    onPropertyChanged(nameof(active_playlist));
                }
            }
        }

        void selectPlaylistCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        void selectPlaylistExecute(object sender, ExecutedRoutedEventArgs e)
        {

        }

    }
}
