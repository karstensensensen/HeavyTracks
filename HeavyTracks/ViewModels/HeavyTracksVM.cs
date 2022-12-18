using Avalonia;
using Avalonia.Controls;
using HeavyTracks.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace HeavyTracks.ViewModels
{
    public class HeavyTracksVM : ViewModelBase
    {
        public HeavyTracksVM()
        {
            if (!weigher.loadCachedId(creds_file))
            {
                // TODO: should display an error popup requesting a client id? 
                // this is only hit if something major goes wrong, or if the user manually edited the creds file.
            }

            if (!weigher.loadCachedToken(creds_file))
            {
                weigher.newUserToken();
                weigher.cacheUserToken(creds_file);
            }

            Playlists = weigher.getPlaylists();

        }



        readonly string creds_file = Design.IsDesignMode ? "design_creds.toml" : "creds.toml";

        SpotifyWeigher weigher = new();
        List<Playlist> Playlists { get; set; }

        Playlist? selected_playlist = null;
        Playlist? SelectedPlaylist
        {
            get => selected_playlist;
            set
            {
                this.RaiseAndSetIfChanged(ref selected_playlist, value);
                SelectedPlaylistTracks = weigher.getPlaylistTracks(selected_playlist!);

                this.RaisePropertyChanged(nameof(SelectedPlaylistTracks));
            }
        }

        List<Track> SelectedPlaylistTracks { get; set; }


    }
}
