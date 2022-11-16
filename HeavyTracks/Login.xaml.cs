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

namespace HeavyTracks
{

    public class SpotifyLogin : ICommand
    {
        public Window? fallback_window = null;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            Trace.WriteLine("Login Attempt");

            if(fallback_window != null)
            {
                App.Current.MainWindow.Close();
                fallback_window.Show();
            }
        }
    }

    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public static SpotifyLogin spotify_login = new();

        public Login()
        {
            InitializeComponent();
        }
    }
}
