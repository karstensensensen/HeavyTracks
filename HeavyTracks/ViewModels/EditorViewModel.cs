using HeavyTracks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeavyTracks.ViewModels
{
    public class EditorViewModel: BaseViewModel
    {
        PlaylistWeigher model;

        public EditorViewModel(PlaylistWeigher _model)
        {
            model = _model;
        }

    }
}
