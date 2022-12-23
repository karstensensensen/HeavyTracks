using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HeavyTracks.Models
{
    public class SpotifyWeigherException : Exception
    {
        public SpotifyWeigherException(string msg = "") : base(msg) { }

        public SpotifyWeigherException(string msg, HttpResponseMessage resp)
            : base($"{msg}\n" +
                  $"Status Code: {resp.StatusCode}\n" +
                  $"Body: {new StreamReader(resp.Content.ReadAsStream()).ReadToEnd()}\n")
        { }
    }
}
