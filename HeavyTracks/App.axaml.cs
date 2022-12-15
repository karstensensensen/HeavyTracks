using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HeavyTracks.ViewModels;
using HeavyTracks.Views;

namespace HeavyTracks
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new HeavyTracksWindow
                {
                    DataContext = new HeavyTracksVM(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
