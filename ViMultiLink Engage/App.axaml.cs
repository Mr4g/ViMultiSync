using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ViMultiSync.Services;
using ViMultiSync.ViewModels;
using ViMultiSync.Views;

namespace ViMultiSync
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {

            // Initialize the dependencies
            var statusInterface = new DummyStatusInterfaceService();
            var mainViewModel = new MainWindowViewModel(statusInterface);



            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}