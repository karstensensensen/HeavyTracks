using HeavyTracks.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Diagnostics;

namespace HeavyTracks.Models
{
    public class EditorModel
    {

        public void login()
        {
            var server = new EmbedIOAuthServer(new Uri("http://localhost:8080/callback"), 8080);

            Trace.WriteLine("EEE");
        }
    }
}
