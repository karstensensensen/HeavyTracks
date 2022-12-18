using HeavyTracks.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HeavyTracks.ViewModels
{
    public class HeavyTracksVM : ViewModelBase
    {
        public HeavyTracksVM()
        {
            if (!Avalonia.Controls.Design.IsDesignMode)
            {
                weigher.newUserToken();
                Playlists = weigher.getPlaylists();
            }
        }

        SpotifyWeigher weigher = new("85bfa24f31c2414eba026ef1bea0c575");
        List<Playlist> Playlists { get; set; }
    }
}
