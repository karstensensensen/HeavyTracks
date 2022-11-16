using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HeavyTracks
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            SpotifyContext.initialize("85bfa24f31c2414eba026ef1bea0c575");
            SpotifyContext.newUserToken();
            SpotifyContext.getUserId();
            SpotifyContext.getPlaylists();
        }

    }
}
