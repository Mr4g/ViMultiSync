using System.Drawing;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using ViMultiSync.Views;

namespace ViMultiSync.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private UserControl _activeUserControl;

        public UserControl ActivePage
        {
            get => _activeUserControl; 
            set => this.RaiseAndSetIfChanged(ref _activeUserControl, value);
        }


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
