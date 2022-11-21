using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeavyTracks.ViewModels
{
    public class MainViewModel: BaseViewModel
    {
        public BaseViewModel CurrentModel { get; }

        public MainViewModel()
        {
            CurrentModel = new SpotifyWeigherEditorVM(new("85bfa24f31c2414eba026ef1bea0c575"));
        }
    }
}
