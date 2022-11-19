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
        EditorModel model;

        public EditorViewModel(EditorModel _model)
        {
            model = _model;
            model.login();
        }

    }
}
