using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ViSyncMaster.Services;
using ViSyncMaster.ViewModels;
using ViSyncMaster.Views;

namespace ViSyncMaster
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {

            // Konfiguracja œcie¿ki do SCADY
            ScadaProcessManager.Instance.StartPath = @"C:\ViSM\SCADA\W16 SCADA.lnk";
            ScadaProcessManager.Instance.WindowTitleMatch = "W16 SCADA";

            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }

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