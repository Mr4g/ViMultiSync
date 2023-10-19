using System.Drawing;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ViMultiSync.Views;

namespace ViMultiSync.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {

        private UserControl _activeUserControl;

        [ObservableProperty]
        private bool downtimePanelIsOpen = false;

        public UserControl ActivePage
        {
            get => _activeUserControl;
            set => SetProperty(ref _activeUserControl, value);
        }

        [RelayCommand]
        public void DowntimePanelButtonPressed() => DowntimePanelIsOpen ^= true;

        public void LoadPageDowntime()
        {
            ActivePage = new UCDowntime();
        }

        public void LoadPageSetting()
        {
            ActivePage = new UCSetting();
        }

        public void LoadPageMaintenace()
        {
            ActivePage = new UCMaintenance();
        }
        public void LoadPageLogistic()
        {
            ActivePage = new UCLogistic();
        }
        public void LoadPageSap()
        {
            ActivePage = new UCBrowser();
        }
        public void LoadPageSplunk()
        {
        }

    }
}
