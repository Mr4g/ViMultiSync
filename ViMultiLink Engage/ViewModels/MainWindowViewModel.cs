using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using ViMultiSync.DataModel;
using ViMultiSync.Services;
using ViMultiSync.Views;

namespace ViMultiSync.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region Private Memebers

        private IStatusInterfaceService mStatusInterfaceService;

        #endregion

        #region Public Properties


        [ObservableProperty]
        private bool _downtimePanelIsOpen = false;

        [ObservableProperty] 
        private ObservableGroupedCollection<string, DowntimePanelItem> _statusPanel = default!;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimePanelButtonText))]
        private DowntimePanelItem? _selectedDowntimePanelItem;

        public string DowntimePanelButtonText => SelectedDowntimePanelItem?.ShortText ?? "Downtime";
        #endregion

        private UserControl _activeUserControl;

        public UserControl ActivePage
        {
            get => _activeUserControl;
            set => SetProperty(ref _activeUserControl, value);
        }

        #region Public Command

        [RelayCommand]
        public void DowntimePanelButtonPressed() => DowntimePanelIsOpen ^= true;

        [RelayCommand]
        private void DowntimePanelItemPressed(DowntimePanelItem item)
        {
            // Update the selected item 
            SelectedDowntimePanelItem = item;

            // Close the menu 
            DowntimePanelIsOpen = false;
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            // Get th channel configuration data
            var statusPanel = await mStatusInterfaceService.GetDowntimePanelAsync();

            // Create a grouping from the flat data
            StatusPanel =
                new ObservableGroupedCollection<string, DowntimePanelItem>(
                    statusPanel.GroupBy(item => item.Status));
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

        internal Task LoadSettingsCommandExecute()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default contructor
        /// </summary>
        /// <param name="statusInterfaceService">The status interface service</param>

        public MainWindowViewModel(IStatusInterfaceService statusInterfaceService)
        {
            mStatusInterfaceService = statusInterfaceService;
        }

        /// <summary>
        /// Design - time constructor
        /// </summary>
        public MainWindowViewModel()
        {
            mStatusInterfaceService = new DummyStatusInterfaceService();
        }

        #endregion


    }
}
