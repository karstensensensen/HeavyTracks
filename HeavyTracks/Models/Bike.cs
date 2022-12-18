using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeavyTracks.Models
{
    public class Bike : UserControl
    {
        public int Wheels { get; set; } = 25;

        public override string ToString()
        {
            return $"This bika has {Wheels} weels !!! UwU UwU ~owo~";
        }
    }
}
