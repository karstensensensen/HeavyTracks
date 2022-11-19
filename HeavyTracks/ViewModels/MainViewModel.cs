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
        BaseViewModel CurrentModel { get; }

        MainViewModel()
        {
            CurrentModel = new EditorViewModel(new());
        }
    }
}
