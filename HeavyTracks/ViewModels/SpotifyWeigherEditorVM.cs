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
        public string PlaylistName { get; set; } = "NOT SELECTED";
        public string PlaylistIcon { get; set; } = "";

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

                    PlaylistName = active_playlist?.name ?? "PLAYLIST";
                    
                    onPropertyChanged(nameof(tracks));
                    onPropertyChanged(nameof(active_playlist));
                    onPropertyChanged(nameof(PlaylistName));

                    PlaylistIcon = weigher.getPlaylistImgUrl(active_playlist) ?? "";
                    onPropertyChanged(nameof(PlaylistIcon));
                }
            }
        }

        void selectPlaylistCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        void selectPlaylistExecute(object sender, ExecutedRoutedEventArgs e)
        {

        }

    }
}
