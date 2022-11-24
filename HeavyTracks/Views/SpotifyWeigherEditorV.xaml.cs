using HeavyTracks.Models;
using HeavyTracks.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HeavyTracks.Views
{
    /// <summary>
    /// Interaction logic for EditorView.xaml
    /// </summary>
    public partial class SpotifyWeigherEditor : UserControl
    {
        public SpotifyWeigherEditor()
        {
            InitializeComponent();
        }

        private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
