using Avalonia;
using Avalonia.Controls;
using HeavyTracks.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace HeavyTracks.ViewModels
{
    public class HeavyTracksVM : ViewModelBase
    {
        public HeavyTracksVM()
        {
            if (!Design.IsDesignMode)
            {
                if (!weigher.loadClientId(id_file))
                {
                    // TODO: should display an error popup requesting a client id? 
                    // this is only hit if something major goes wrong, or if the user manually edited the creds file.
                }

                weigher.beginSession();

                Playlists = weigher.getPlaylists();
            }
        }



        readonly string id_file = "creds.toml";

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

        List<Track>? SelectedPlaylistTracks { get; set; }

        public void apply()
        {
            weigher.pushTracks(SelectedPlaylistTracks, SelectedPlaylist, true);
        }

        public void sync()
        {
            SelectedPlaylistTracks = weigher.getPlaylistTracks(selected_playlist);
            this.RaisePropertyChanged(nameof(SelectedPlaylistTracks));
        }

    }
}
