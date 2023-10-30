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

        #region Panel is open

        [ObservableProperty]
        private int _rowForSettingPanel;

        [ObservableProperty]
        private int _rowForMaintenancePanel;

        [ObservableProperty]
        private int _rowForLogisticPanel;

        [ObservableProperty]
        private int _rowForReasonDowntimePanel;

        [ObservableProperty]
        private bool _downtimePanelIsOpen = false;

        [ObservableProperty]
        private bool _settingPanelIsOpen = false;

        [ObservableProperty]
        private bool _maintenancePanelIsOpen = false;

        [ObservableProperty]
        private bool _logisticPanelIsOpen = false;

        [ObservableProperty]
        private bool _reasonDowntimePanelIsOpen = false;

        [ObservableProperty]
        private bool _splunkPanelIsOpen = false;

        [ObservableProperty]
        private bool _controlPanelVisible = false;

        [ObservableProperty]
        private bool _serviceArrivalVisible = false;

        [ObservableProperty]
        private bool _actualStatusButtonIsVisible = false;

        [ObservableProperty]
        private string _actualStatusButtonText = "WYBIERZ STATUS";

        [ObservableProperty]
        private string _actualStatusColor = "BRAK";

        [ObservableProperty]
        private string _serviceArrivalButtonText = "BRAK";

        [ObservableProperty]
        private string _serviceArrivalColor = "#FFA000";



        #endregion

        #region GroupedCollection


        [ObservableProperty] 
        private ObservableGroupedCollection<string, DowntimePanelItem> _statusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, SettingPanelItem> _settingStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, MaintenancePanelItem> _maintenanceStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, LogisticPanelItem> _logisticStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, ReasonDowntimePanelItem> _reasonDowntimeStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, SplunkPanelItem> _splunkPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, ServiceArrivalPanelItem> _serviceArrivalPanel = default!;

        #endregion

        #region ProportyChangedFor

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimePanelButtonText))]
        private DowntimePanelItem? _selectedDowntimePanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SettingPanelButtonText))]
        private SettingPanelItem? _selectedSettingPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaintenancePanelButtonText))]
        private MaintenancePanelItem? _selectedMaintenancePanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LogisticPanelButtonText))]
        private LogisticPanelItem? _selectedLogisticPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ReasonDowntimePanelButtonText))]
        private ReasonDowntimePanelItem? _selectedReasonDowntimePanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SplunkPanelButtonText))]
        private SplunkPanelItem? _selectedSplunkPanelItem;



        #endregion

        public string DowntimePanelButtonText => SelectedDowntimePanelItem?.Name ?? "Downtime";

        public string SettingPanelButtonText => SelectedSettingPanelItem?.Name?? "Setting";

        public string MaintenancePanelButtonText => SelectedMaintenancePanelItem?.Name ?? "Maintenance";

        public string LogisticPanelButtonText => SelectedLogisticPanelItem?.Name ?? "Logistic";

        public string ReasonDowntimePanelButtonText => SelectedReasonDowntimePanelItem?.Name ?? "Reason Downtime";

        public string SplunkPanelButtonText => SelectedSplunkPanelItem?.Name ?? "Splunk";
        #endregion

        private UCBrowser _activeUserControl;

        public UCBrowser ActivePage
        {
            get => _activeUserControl;
            set => SetProperty(ref _activeUserControl, value);
        }

        #region Public Command

        

        [RelayCommand]
        public void DowntimePanelButtonPressed() => DowntimePanelIsOpen ^= true;

        [RelayCommand]
        public void SettingPanelButtonPressed() => SettingPanelIsOpen ^= true;

        [RelayCommand]
        public void MaintenancePanelButtonPressed() => MaintenancePanelIsOpen ^= true;

        [RelayCommand]
        public void LogisticPanelButtonPressed() => LogisticPanelIsOpen ^= true;

        [RelayCommand]
        public void ReasonDowntimePanelButtonPressed() => ReasonDowntimePanelIsOpen ^= true;

        [RelayCommand]
        public void SplunkPanelButtonPressed() => SplunkPanelIsOpen ^= true;



        [RelayCommand]
        private void DowntimePanelItemPressed(DowntimePanelItem item)
        {
            const string colorDowntime = "#DC4E41";

            // Update the selected item 
            SelectedDowntimePanelItem = item;

            ServiceArrivalVisible = true;

            // Close the menu 
            DowntimePanelIsOpen = false;

            ChangePropertyButtonStatus(colorDowntime, item.Status);
        }

        [RelayCommand]
        private void SettingPanelItemPressed(SettingPanelItem item)
        {
            const string colorSetting = "#EE82EE";

            // Update the selected item 
            SelectedSettingPanelItem = item;

            // Close the menu 
            SettingPanelIsOpen = false;

            ChangePropertyButtonStatus(colorSetting, item.Status);
        }

        [RelayCommand]
        private void MaintenancePanelItemPressed(MaintenancePanelItem item)
        {
            const string colorMaintenance = "#81EEEE";

            // Update the selected item 
            SelectedMaintenancePanelItem = item;

            // Close the menu 
            MaintenancePanelIsOpen = false;

            ChangePropertyButtonStatus(colorMaintenance, item.Status);
        }

        [RelayCommand]
        private void LogisticPanelItemPressed(LogisticPanelItem item)
        {
            const string colorLogistic = "#E1D747";
            // Update the selected item 
            SelectedLogisticPanelItem = item;

            // Close the menu 
            LogisticPanelIsOpen = false;

            ChangePropertyButtonStatus(colorLogistic, item.Status);
        }

        [RelayCommand]
        private void ReasonDowntimePanelItemPressed(ReasonDowntimePanelItem item)
        {
            // Update the selected item 
            SelectedReasonDowntimePanelItem = item;

            // Close the menu 
            ReasonDowntimePanelIsOpen = false;
            
        }
        [RelayCommand]
        private void SplunkPanelItemPressed(SplunkPanelItem item)
        {
            // Update the selected item 
            SelectedSplunkPanelItem = item;

            // Close the menu 
            LoadPageSplunk(item.Link);
            SplunkPanelIsOpen = false;
            ControlPanelVisible = false;
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            // Get th channel configuration data
            var statusPanel = await mStatusInterfaceService.GetDowntimePanelAsync();
            var settingStatusPanel = await mStatusInterfaceService.GetSettingPanelAsync();
            var maintenanceStatusPanel = await mStatusInterfaceService.GetMaintenancePanelAsync();
            var logisticStatusPanel = await mStatusInterfaceService.GetLogisticPanelAsync();
            var reasonDowntimeStatusPanel = await mStatusInterfaceService.GetReasonDowntimePanelAsync();
            var splunkStatusPanel = await mStatusInterfaceService.GetSplunkPanelAsync();
            var serviceArrivalPanel = await mStatusInterfaceService.GetServiceArrivalPanelAsync();


            // Create a grouping from the flat data
            StatusPanel =
                new ObservableGroupedCollection<string, DowntimePanelItem>(
                    statusPanel.GroupBy(item => item.Status));

            SettingStatusPanel =
                new ObservableGroupedCollection<string, SettingPanelItem>(
                    settingStatusPanel.GroupBy(item => item.Name));

            RowForSettingPanel = LoadSizeOfGrid(settingStatusPanel.Count);


            MaintenanceStatusPanel =
                new ObservableGroupedCollection<string, MaintenancePanelItem>(
                    maintenanceStatusPanel.GroupBy(item => item.Name));

            RowForMaintenancePanel = LoadSizeOfGrid(maintenanceStatusPanel.Count);

            LogisticStatusPanel =
                new ObservableGroupedCollection<string, LogisticPanelItem>(
                    logisticStatusPanel.GroupBy(item => item.Name));

            RowForLogisticPanel = LoadSizeOfGrid(logisticStatusPanel.Count);

            ReasonDowntimeStatusPanel =
                new ObservableGroupedCollection<string, ReasonDowntimePanelItem>(
                    reasonDowntimeStatusPanel.GroupBy(item => item.Name));

            RowForReasonDowntimePanel = LoadSizeOfGrid(reasonDowntimeStatusPanel.Count);

            SplunkPanel =
                new ObservableGroupedCollection<string, SplunkPanelItem>(
                    splunkStatusPanel.GroupBy(item => item.Group));

            ServiceArrivalPanel =
                new ObservableGroupedCollection<string, ServiceArrivalPanelItem>(
                    serviceArrivalPanel.GroupBy(item => item.Name));


        }

        private int LoadSizeOfGrid(int numberOfElements)
        {
            int maxColumns = 3; 

            int rows = (int)Math.Ceiling((double)numberOfElements / maxColumns);

            return rows;
        }

        public void ChangePropertyButtonStatus(string colorButton, string textButton)
        {
            ActualStatusButtonIsVisible = true;
            ActualStatusButtonText = textButton;
            ActualStatusColor = colorButton;
            ControlPanelVisible = false;
        }


        public void LoadPageSap()
        {
            string sapUrl = "http://ps093w05.viessmann.net:51300/pod-me/com/sap/me/wpmf/client/template.jsf?WORKSTATION=WORK_CENTER_TOUCH_TEST&SOFT_KEYBOARD=true&ACTIVITY_ID=ZVI_WC_POD_COPPER&sap-lsf-PreferredRendering=standards#";
            if (ActivePage == null)
            {
                ActivePage = new UCBrowser(sapUrl);
            }
            else
            {
                ActivePage.ChangeBrowserAddress($"{sapUrl}");
            }
        }
        public void LoadPageSplunk(string link)
        {
            if (ActivePage == null)
            {
                ActivePage = new UCBrowser(link);
            }
            else
            {
                ActivePage.ChangeBrowserAddress($"{link}");
            }
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
