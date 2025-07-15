using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using ViSyncMaster.DataModel;
using ViSyncMaster.Entitys;
using ViSyncMaster.Repositories;
using ViSyncMaster.Services;
using ViSyncMaster.Views;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using ScreenCapturerNS;
using Bitmap = System.Drawing.Bitmap;
using Icon = MsBox.Avalonia.Enums.Icon;
using Path = System.IO.Path;
using Timer = System.Threading.Timer;
using ViSyncMaster.AuxiliaryClasses;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using Serilog;
using System.Reflection;
using System.Linq;
using ViSyncMaster.Handlers;
using System.Data;
using ViSyncMaster.DeepCopy;
using System.Text.Json;
using ViSyncMaster.Services.Test;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ViSyncMaster.Heleprs;
using System.Runtime.InteropServices;
using ViSyncMaster.SystemParameters;


namespace ViSyncMaster.ViewModels
{
    public partial class MainWindowViewModel : ObservableValidator
    {
        #region Private Memebers
        private readonly SQLiteDatabase _database;

        private IStatusInterfaceService mStatusInterfaceService;
        private Dictionary<Type, object> repositories = new Dictionary<Type, object>();
        private readonly SharedDataService _sharedDataService;
        public static AppConfigData appConfig { get; private set; }
        public static ConfigMqtt mqttConfig { get; private set; }
        private Timer _timer;
        private Timer _timerForReSendMassage;
        private Timer _timerForLoadStatuses;
        private DispatcherTimer _timerForVacuum;
        private GenericMessageFromPlc _messageFromPlc;
        private TestingFailedMessage _messageToSplunkFailed;
        private TestingPassedMessage _messageToSplunkPassed;
        private ConnectedMessage _messageToSplunkConnected;
        private Rs232DataProcessor _rs232Processor;
        private MessagePgToSplunk _messageToSplunkPg;
        private int _machineStatusCounter = 6;
        private GenericSplunkLogger<IEntity> _splunkLogger;
        private WifiParameters _wifiParameters;
        private AnyDeskParameters _anyDeskParameters;   

        private DispatcherTimer _timerScheduleForLogging;
        private TaskCompletionSource<bool> _dataIsSendingToSplunkCompletionSource = new TaskCompletionSource<bool>();

        private PingService pingService;
        private MachineStatus _pendingMachineStatus;

        string screenshotPath = "C:/zrzut_ekranu.png"; // Ścieżka, gdzie zostanie zapisany zrzut ekranu
        string imgurClientId = "0fe6e59673311dc"; // Zastąp wartością swojego Client ID zarejestrowanego na Imgur
        string imagePath = @"\screen\SAP_ERROR.png";
        private string sapUrl;
        private string adaptronicUrl;
        private string googleDiskUrl;
        private string googleInstructionUrl;
        private string googleTargetPlanUrl;


        public event EventHandler? ResultTableUpdate;

        private int _counterTest = 0;

        private Dictionary<string, string> PanelActionMapping;

        private string brokerHost;
        private int brokerPort;
        string clientId;
        string token;

        private List<CallForServicePanelItem> _allCallForServicePanelData = new List<CallForServicePanelItem>();

        /// <summary>
        /// Klasa odpowiedzialna za wysyłanie danych, zarządzanie kolejką wiadomości oraz zapisywanie i odczytywanie statusów maszyn.
        /// </summary>
        /// <remarks>
        /// - <paramref name="_repository">Repozytorium</paramref> zarządza zapisywaniem i wczytywaniem statusów maszyn do/z pliku.
        /// - <paramref name="_messageQueue">Kolejka wiadomości</paramref> przechowuje wiadomości do wysłania w przypadku braku połączenia.
        /// - <paramref name="_messageSender">Wysyłanie wiadomości</paramref> odpowiada za wysyłanie wiadomości do zewnętrznego systemu lub ich kolejkowanie w przypadku braku połączenia.
        /// - <paramref name="_machineStatusService">Serwis statusów maszyn</paramref> zarządza tworzeniem, aktualizowaniem i kończeniem statusów maszyn.
        /// </remarks>

        private readonly GenericRepository<MachineStatus> _repositoryMachineStatus;
        private readonly GenericRepository<MachineStatus> _repositoryMachineStatusQueue;
        private readonly GenericRepository<MachineStatus> _repositoryTestingResultQueue;
        private readonly GenericRepository<MachineStatus> _repositoryTestingResult;
        private readonly GenericRepository<ProductionEfficiency> _repositoryProductionEfficiency;
        private readonly GenericRepository<FirstPartModel> _repositoryFirstPartData;
        private readonly MessageQueue _messageQueue;
        private readonly MessageSender _messageSender;
        private readonly MachineStatusService _machineStatusService;
        private ResultTableView _resultTableView;
        private FormFirstPartView _firstPartView;

        private readonly SplunkMessageHandler _splunkMessageHandler;

        #endregion
        private bool sentMessageWithTrue = false;
        IEntity _lastMessage;

        #region Event 

        public event EventHandler<string>? FocusRequested;


        #endregion

        #region Public Properties

        #region Setting Properties

        [ObservableProperty]
        public bool _vacuumPanelAvailable;

        #endregion

        #region Panel is open

        [ObservableProperty]
        private bool _isPasswordProtected;

        [ObservableProperty]
        private string _enteredPassword;


        [ObservableProperty]
        private string _enteredLogin;

        [ObservableProperty]
        private string _loginLabel;

        [ObservableProperty]
        private string _ssid;

        [ObservableProperty]
        private string _anyDeskId;

        [ObservableProperty]
        public bool _isActiveStatus;// lub false, zależnie od logiki

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [RegularExpression(@"^(Line[1-6]Pump[1-6])$", ErrorMessage = "Niepoprawny numer stacji")]
        [NotifyCanExecuteChangedFor(nameof(ClearButtonPressedCommand), nameof(SendMessageToPlcCommand))]
        [NotifyPropertyChangedFor(nameof(CanSendDataToPlc))]
        private string _numberStation;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [NotifyCanExecuteChangedFor(nameof(ClearButtonPressedCommand), nameof(SendMessageToPlcCommand))]
        [NotifyPropertyChangedFor(nameof(CanSendDataToPlc))]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Niepoprawny GNV.")]
        private string _vinHeatPump;

        [ObservableProperty]
        private int _rowForSettingPanel;

        [ObservableProperty]
        private int _rowForDowntimePanel;

        [ObservableProperty]
        private int _rowForMaintenancePanel;

        [ObservableProperty]
        private int _rowForLogisticPanel;

        [ObservableProperty]
        private int _rowForProductionIssuesPanel;

        [ObservableProperty]
        private int _rowForReasonDowntimeMechanicalPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonElectricPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonLiderPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonKptjPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonPlatePanel;

        [ObservableProperty]
        private int _rowForCallForServicePanel;

        [ObservableProperty]
        private int _rowForServiceArrivalPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonBindownicaPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonSC200Panel;

        [ObservableProperty]
        private int _rowForDowntimeReasonZebraPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonWtryskarkaPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonBradyPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonWiazarkaPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonKomaxPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonURPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonZFPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonTesterPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonDozownikPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonWalcarkaPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonTesterWodnyPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonLumbergPanel;

        [ObservableProperty]
        private bool _downtimePanelIsOpen = false;

        [ObservableProperty]
        private bool _settingPanelIsOpen = false;

        [ObservableProperty]
        private bool _optionsPanelIsOpen = false;

        [ObservableProperty]
        private bool _infoPanelIsOpen = false;

        [ObservableProperty]
        private bool _maintenancePanelIsOpen = false;

        [ObservableProperty]
        private bool _logisticPanelIsOpen = false;

        [ObservableProperty]
        private bool _productionIssuesPanelIsOpen = false;

        [ObservableProperty]
        private bool _ReasonDowntimeMechanicalPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonElectricPanelIsOpen = false;

        [ObservableProperty]
        private bool _DowntimeReasonLiderPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonKptjPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonPlatePanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonBindownicaPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonSC200PanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonZebraPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonWtryskarkaPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonBradyPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonWiazarkaPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonKomaxPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonURPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonZFPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonTesterPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonDozownikPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonWalcarkaPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonTesterWodnyPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonLumbergPanelIsOpen = false;

        [ObservableProperty]
        private bool _callForServicePanelIsOpen = false;

        [ObservableProperty]
        private bool _serviceArrivalPanelIsOpen = false;

        [ObservableProperty]
        private bool _splunkPanelIsOpen = false;

        [ObservableProperty]
        private bool _warrningPanelIsOpen = false;

        [ObservableProperty]
        private bool _loginPanelIsOpen = false;

        [ObservableProperty]
        private bool _vacuumPanelIsOpen = false;

        [ObservableProperty]
        private bool _controlPanelVisible = false;

        [ObservableProperty]
        private bool _vacuumButtonIsVisible;

        [ObservableProperty]
        private bool _serviceCalled = false;

        [ObservableProperty]
        private bool _serviceArrival = false;

        [ObservableProperty]
        private bool _callForServiceButtonIsVisible = false;

        [ObservableProperty]
        private bool _actualStatusButtonIsVisible = false;

        [ObservableProperty]
        private bool _serviceArrivalButtonIsVisible = false;

        [ObservableProperty]
        private bool _vacuumProgressBarIsVisible;

        [ObservableProperty]
        private string _actualStatusButtonText = "WYBIERZ STATUS";

        [ObservableProperty]
        private string _actualStatusColor = "BRAK";

        [ObservableProperty]
        private string _callForServiceButtonText = "WEZWIJ UR";

        [ObservableProperty]
        private string _serviceArrivalButtonText = "OCZEKIWANIE NA UR";

        [ObservableProperty]
        private string _warrningNoticeText = "ERROR";

        [ObservableProperty]
        private string _barOnTopApp = "DZIAŁ/LOKALIZACJA";

        [ObservableProperty]
        private string _warrningNoticeColor = "#FFA000";

        [ObservableProperty]
        private string _callForServiceColor = "#FFA000";

        [ObservableProperty]
        private string _serviceArrivalButtonColor = "#DC4E41";

        [ObservableProperty]
        private bool _downtimeIsActive = false;

        [ObservableProperty]
        private bool _dataIsSendingToSplunk;
        [ObservableProperty]
        private bool _openSerialPortButtonIsVisible;
        [ObservableProperty]
        private bool _adaptronicButtonIsVisible;
        [ObservableProperty]
        private bool _googleDriveButtonIsVisible;
        [ObservableProperty]
        private bool _instructionButtonIsVisible;
        [ObservableProperty]
        private bool _targetPlanButtonIsVisible;
        [ObservableProperty]
        private bool _userButtonIsVisible;

        [ObservableProperty] private bool isTimeStampFromiPC = true;

        [ObservableProperty] private bool isLoginToApp;

        [ObservableProperty] private string _timerkeeperStatus = "00:00:00";

        [ObservableProperty] private string _timerkeeperService = "00:00:00";

        [ObservableProperty] private double _progressBarValue = 100;

        [ObservableProperty] private double _pumpFourValue = 2.99;

        [ObservableProperty] private TimeSpan _remainingVacuumTime;

        [ObservableProperty] private TimeSpan _currentTime;

        [ObservableProperty]
        private ObservableCollection<MachineStatus> machineStatuses;

        [ObservableProperty]
        private ObservableCollection<MachineStatus> resultTest;


        #endregion

        #region GroupedCollection

        [ObservableProperty]
        private ObservableCollection<ConfigHardwareItem> _deviceInfoPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimePanelItem> _statusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, SettingPanelItem> _settingStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, MaintenancePanelItem> _maintenanceStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, LogisticPanelItem> _logisticStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, ProductionIssuesPanelItem> _productionIssuesStatusPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, ReasonDowntimeMechanicalPanelItem> _ReasonDowntimeMechanicalPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonElectricPanelItem> _downtimeReasonElectricPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonLiderPanelItem> _DowntimeReasonLiderPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonKptjPanelItem> _downtimeReasonKptjPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonPlatePanelItem> _downtimeReasonPlatePanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, SplunkPanelItem> _splunkPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, CallForServicePanelItem> _callForServicePanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, ServiceArrivalPanelItem> _serviceArrivalPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonBindownicaPanelItem> _downtimeReasonBindownicaPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonSC200PanelItem> _downtimeReasonSC200Panel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonZebraPanelItem> _downtimeReasonZebraPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonWtryskarkaPanelItem> _downtimeReasonWtryskarkaPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonBradyPanelItem> _downtimeReasonBradyPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonWiazarkaPanelItem> _downtimeReasonWiazarkaPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonKomaxPanelItem> _downtimeReasonKomaxPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonURPanelItem> _downtimeReasonURPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonZFPanelItem> _downtimeReasonZFPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonTesterPanelItem> _downtimeReasonTesterPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonDozownikPanelItem> _downtimeReasonDozownikPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonWalcarkaPanelItem> _downtimeReasonWalcarkaPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonTesterWodnyPanelItem> _downtimeReasonTesterWodnyPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonLumbergPanelItem> _downtimeReasonLumbergPanel = default!;
        #endregion

        #region ProportyChangedFor

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimePanelButtonText))]
        private MachineStatus? _selectedDowntimePanelItem;

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
        [NotifyPropertyChangedFor(nameof(ProductionIssuesPanelButtonText))]
        private ProductionIssuesPanelItem? _selectedProductionIssuesPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ReasonDowntimeMechanicalPanelButtonText))]
        private ReasonDowntimeMechanicalPanelItem? _selectedReasonDowntimeMechanicalPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimeReasonElectricPanelButtonText))]
        private DowntimeReasonElectricPanelItem? _selectedDowntimeReasonElectricPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimeReasonLiderPanelButtonText))]
        private DowntimeReasonLiderPanelItem? _selectedDowntimeReasonLiderPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimeReasonKptjPanelButtonText))]
        private DowntimeReasonKptjPanelItem? _selectedDowntimeReasonKptjPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimeReasonPlatePanelButtonText))]
        private DowntimeReasonPlatePanelItem? _selectedDowntimeReasonPlatePanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CallForServicePanelButtonText))]
        private CallForServicePanelItem? _selectedCallForServicePanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ServiceArrivalPanelButtonText))]
        private ServiceArrivalPanelItem? _selectedServiceArrivalPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SplunkPanelButtonText))]
        private SplunkPanelItem? _selectedSplunkPanelItem;



        #endregion

        public string DowntimePanelButtonText => SelectedDowntimePanelItem?.Name ?? "Downtime";
        public string SettingPanelButtonText => SelectedSettingPanelItem?.Name ?? "Setting";
        public string MaintenancePanelButtonText => SelectedMaintenancePanelItem?.Name ?? "Maintenance";
        public string LogisticPanelButtonText => SelectedLogisticPanelItem?.Name ?? "Logistic";
        public string ProductionIssuesPanelButtonText => SelectedProductionIssuesPanelItem?.Name ?? "ProductionIssues";
        public string ReasonDowntimeMechanicalPanelButtonText => SelectedReasonDowntimeMechanicalPanelItem?.Name ?? "Reason Downtime";
        public string DowntimeReasonElectricPanelButtonText => SelectedDowntimeReasonElectricPanelItem?.Name ?? "Downtime Reason Electric";
        public string DowntimeReasonLiderPanelButtonText => SelectedDowntimeReasonLiderPanelItem?.Name ?? "Downtime Reason Setting";
        public string DowntimeReasonKptjPanelButtonText => SelectedDowntimeReasonKptjPanelItem?.Name ?? "Downtime Reason Kptj";
        public string DowntimeReasonPlatePanelButtonText => SelectedDowntimeReasonPlatePanelItem?.Name ?? "Downtime Reason Plate";
        public string CallForServicePanelButtonText => SelectedCallForServicePanelItem?.Name ?? "Call For Service";
        public string ServiceArrivalPanelButtonText => SelectedServiceArrivalPanelItem?.Name ?? "Service Arrival";
        public string SplunkPanelButtonText => SelectedSplunkPanelItem?.Name ?? "Splunk";

        public bool CanDeleteTheNumberStationAndGnv =>
            HasNumberStation ||
            HasNumberGnv ||
            ProgressBarValue < 100;


        public bool CanSendDataToPlc =>
            HasNumberStation &&
            HasNumberGnv &&
            !HasErrors;

        private bool HasNumberStation => !string.IsNullOrEmpty(NumberStation);
        private bool HasNumberGnv => !string.IsNullOrEmpty(VinHeatPump);

        public ObservableCollection<MachineStatus> ResultTestList { get; internal set; }

        #endregion

        private UCBrowser _activeUserControl;

        //public UCBrowser ActivePage
        //{
        //    get => _activeUserControl;
        //    set => SetProperty(ref _activeUserControl, value);
        //}

        [ObservableProperty]
        private UserControl _activePage;

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
        public void ProductionIssuesPanelButtonPressed() => ProductionIssuesPanelIsOpen ^= true;

        [RelayCommand]
        public void ReasonDowntimeMechanicalPanelButtonPressed() => ReasonDowntimeMechanicalPanelIsOpen ^= true;

        [RelayCommand]
        public void DowntimeReasonElectricPanelButtonPressed() => DowntimeReasonElectricPanelIsOpen ^= true;

        [RelayCommand]
        public void DowntimeReasonLiderPanelButtonPressed() => DowntimeReasonLiderPanelIsOpen ^= true;

        [RelayCommand]
        public void DowntimeReasonKptjPanelButtonPressed() => DowntimeReasonKptjPanelIsOpen ^= true;

        [RelayCommand]
        public void DowntimeReasonPlatePanelButtonPressed() => DowntimeReasonPlatePanelIsOpen ^= true;

        [RelayCommand]
        public void CallForServicePanelButtonPressed() => CallForServicePanelIsOpen ^= true;

        [RelayCommand]
        public void SplunkPanelButtonPressed() => SplunkPanelIsOpen ^= true;


        private void UpdateCallForServicePanel(int stepOfStatus, MachineStatus machineStatus)
        {
            List<CallForServicePanelItem> filteredItems;

            // Krok 0 i status "PŁYTA" → tylko WEZWIJ SERWIS + EXIT
            if (stepOfStatus == 0
                && !string.IsNullOrEmpty(machineStatus.Status)
                && machineStatus.Status.Equals("PŁYTA", StringComparison.InvariantCultureIgnoreCase))
            {
                filteredItems = _allCallForServicePanelData
                    .Where(item =>
                        item.Name == "CallForService" &&
                        item.Status.Equals("WEZWIJ SERWIS", StringComparison.InvariantCultureIgnoreCase)
                    )
                    // doklejamy EXIT
                    .Concat(_allCallForServicePanelData
                        .Where(item => item.Name == "Exit"))
                    .ToList();
            }
            else
            {
                // Normalne filtrowanie, ale bez CallForService o Statusie "WEZWIJ SERWIS"
                filteredItems = _allCallForServicePanelData
                    .Where(item =>
                        // najpierw wykluczamy CallForService z status WEZWIJ SERWIS
                        !(item.Name == "CallForService"
                          && item.Status.Equals("WEZWIJ SERWIS", StringComparison.InvariantCultureIgnoreCase))
                        &&
                        // a potem oryginalne kryteria po kroku
                        (
                            (stepOfStatus == 0 && (item.Name == "CallForService" || item.Name == "Exit"))
                         || (stepOfStatus == 1 && (item.Name == "ServiceArrival" || item.Name == "DowntimeReason" || item.Name == "Exit"))
                         || (stepOfStatus == 2 && (item.Name == "DowntimeReason" || item.Name == "Exit"))
                        )
                    )
                    .ToList();
            }

            // Grupowanie i przypisanie do widoku
            var groupedItems = filteredItems.GroupBy(item => item.Value);
            CallForServicePanel = new ObservableGroupedCollection<string, CallForServicePanelItem>(groupedItems);
            RowForCallForServicePanel = LoadSizeOfGrid(filteredItems.Count);
        }

        [RelayCommand]
        private async Task ReportMachineDowntime(MachineStatus item)
        {
            UpdateCallForServicePanel(0, item);
            var itemWords = item.Status
                .ToUpperInvariant()
                .Split(new[] { ' ', '&' }, StringSplitOptions.RemoveEmptyEntries);

            if (MachineStatuses.Any(s =>
                itemWords.Any(w => s.Status.ToUpperInvariant().Contains(w))))
            {
                ActualValueForWarrningNoticePopup(item);
                return;
            }
            if (item.Name == "S7.DowntimeReason")
            {
                _pendingMachineStatus.Reason = item.Reason;
                await HandleUnmappedStatus(_pendingMachineStatus);
                _pendingMachineStatus = null;
            }
            else
            {
                CallForServicePanelIsOpen = true;
                ControlPanelVisible = true;
                _pendingMachineStatus = item.DeepCopy();
            }
            //LoadStatuses(this); // Zaktualizuj listę statusów
            ControlPanelVisibility();
        }

        private void ControlPanelVisibility()
        {
            SettingPanelIsOpen = false;
            DowntimePanelIsOpen = false;
            MaintenancePanelIsOpen = false;
            LogisticPanelIsOpen = false;
            ProductionIssuesPanelIsOpen = false;
            DowntimeReasonLiderPanelIsOpen = false;
            DowntimeReasonSC200PanelIsOpen = false;
            DowntimeReasonZebraPanelIsOpen = false;
            DowntimeReasonWtryskarkaPanelIsOpen = false;
            DowntimeReasonBradyPanelIsOpen = false;
            DowntimeReasonWiazarkaPanelIsOpen = false;
            DowntimeReasonKomaxPanelIsOpen = false;
            DowntimeReasonURPanelIsOpen = false;
            DowntimeReasonZFPanelIsOpen = false;
            DowntimeReasonTesterPanelIsOpen = false;
            DowntimeReasonDozownikPanelIsOpen = false;
            DowntimeReasonWalcarkaPanelIsOpen = false;
            DowntimeReasonTesterWodnyPanelIsOpen = false;
            DowntimeReasonBindownicaPanelIsOpen = false;
            DowntimeReasonLumbergPanelIsOpen = false;
        }


        [RelayCommand]
        private async Task ReportMachineStatus(MachineStatus item)
        {
            if (MachineStatuses.Any(status => status.Status == item.Status))
            {
                ActualValueForWarrningNoticePopup(item);
                return;
            }
            if (item.Status == "SZKOLENIE PRACOWNIKA")
                IsActiveStatus = true;
            if (item.Name == "S7.DowntimeReason")
            {
                _pendingMachineStatus.Reason = item.Reason;
                await HandleUnmappedStatus(_pendingMachineStatus);
                _pendingMachineStatus = null;
            }
            else
            {
                var newStatus = item.DeepCopy();
                await _machineStatusService.StartStatus(newStatus);
                var messagePgToSplunk = _splunkMessageHandler.PreparingPgMessageToSplunk(MachineStatuses, newStatus, _machineStatusCounter);
                await _machineStatusService.SendPgMessage((MessagePgToSplunk)messagePgToSplunk);
            }
            LoadStatuses(this); // Zaktualizuj listę statusów
            ControlPanelVisibility();
            ControlPanelVisible = false;
        }
        [RelayCommand]
        private async Task ActualStatusButtonPressed(MachineStatus machineStatus)
        {
            if (machineStatus.StepOfStatus == 0)
            {
                await HandleUnmappedStatus(machineStatus);
                return;
            }
            UpdateCallForServicePanel(machineStatus.StepOfStatus, machineStatus);
            _pendingMachineStatus = machineStatus.DeepCopy();
            CallForServicePanelIsOpen = true;
            ControlPanelVisible = true;
        }

        private void HandleMappedStatus(MachineStatus machineStatus, string panelName)
        {
            // Zapisanie tymczasowego statusu, aby czekać na powód zakończenia
            _pendingMachineStatus = machineStatus.DeepCopy();

            // Aktywacja odpowiedniego panelu
            var property = GetType().GetProperty(panelName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(bool))
            {
                property.SetValue(this, true); // Ustawienie właściwości na true
            }
            else
            {
                Console.WriteLine($"Nie znaleziono właściwości: {panelName}");
            }
            ControlPanelVisible = true;
        }

        private async Task HandleUnmappedStatus(MachineStatus machineStatus)
        {
            IsActiveStatus = false;
            await _machineStatusService.EndStatus(machineStatus); // Kończenie statusu bez powodu
            var messagePgToSplunk = _splunkMessageHandler.PreparingPgMessageToSplunk(MachineStatuses, machineStatus, _machineStatusCounter);
            await _machineStatusService.SendPgMessage((MessagePgToSplunk)messagePgToSplunk);
            ReasonDowntimeMechanicalPanelIsOpen = false;
            DowntimeReasonElectricPanelIsOpen = false;
            DowntimeReasonKptjPanelIsOpen = false;
            DowntimeReasonPlatePanelIsOpen = false;
            ControlPanelVisible = false;
        }
        [RelayCommand]
        private async Task CallForServicePanelItemPressed(CallForServicePanelItem item)
        {
            // Update the selected item 
            if (_pendingMachineStatus != null)
            {
                // Scal brakujące dane z CallForServicePanelItem do _pendingMachineStatus
                await MergePendingMachineStatusWithPanelItem(_pendingMachineStatus, item);

                // Rozpocznij nowy status
                //var updatedStatus = _machineStatusService.StartStatus(_pendingMachineStatus);
                if (_pendingMachineStatus.StepOfStatus > 2)
                    _pendingMachineStatus = null; // Wyczyszczenie tymczasowego statusu po scaleniu
            }
        }
        private async Task MergePendingMachineStatusWithPanelItem(MachineStatus pendingStatus, CallForServicePanelItem panelItem)
        {
            string panelKey = "";
            panelKey = StatusParser.GetPanelKey(pendingStatus.Status);

            if (pendingStatus == null || panelItem == null || panelItem.Name == "Exit")
            {
                CallForServicePanelIsOpen = false;
                return;
            }
            if (panelItem.Name == "CallForService")
            {
                pendingStatus.CallForService = DateTime.Now;
                if (pendingStatus.Status != "PŁYTA")
                    pendingStatus.Status = $"{pendingStatus.Status} & {panelItem.Status}";
                await _machineStatusService.StartStatus(pendingStatus);
                var messagePgToSplunk = _splunkMessageHandler.PreparingPgMessageToSplunk(MachineStatuses, pendingStatus, _machineStatusCounter);
                await _machineStatusService.SendPgMessage((MessagePgToSplunk)messagePgToSplunk);

            }
            if (panelItem.Name == "ServiceArrival")
            {
                pendingStatus.ServiceArrival = DateTime.Now;
                await _machineStatusService.UpdateStatus(pendingStatus);
                var messagePgToSplunk = _splunkMessageHandler.PreparingPgMessageToSplunk(MachineStatuses, pendingStatus, _machineStatusCounter);
                await _machineStatusService.SendPgMessage((MessagePgToSplunk)messagePgToSplunk);
            }
            if (panelItem.Name == "DowntimeReason")
            {
                if (string.IsNullOrEmpty(pendingStatus?.Status) || PanelActionMapping == null)
                {
                    return; // Zatrzymanie dalszego przetwarzania, jeśli status lub mapowanie jest puste
                }

                if (PanelActionMapping.TryGetValue(panelKey, out var panelName))
                {
                    HandleMappedStatus(pendingStatus, panelName);
                }
                else
                {
                    await HandleUnmappedStatus(pendingStatus);
                }
                CallForServicePanelIsOpen = false;
                return;
            }
            CallForServicePanelIsOpen = false;
            ControlPanelVisible = false;
            LoadStatuses(this);
        }

        public static void ShowMessageBox(string message)
        {
            var box = MessageBoxManager.GetMessageBoxCustom(
                new MessageBoxCustomParams
                {
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "OK", },
                    },
                    ContentTitle = "Informacja",
                    ContentMessage = message,
                    Icon = Icon.Wifi,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    MaxWidth = 500,
                    MaxHeight = 800,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInCenter = true,
                    Topmost = true,
                });

            box.ShowAsync();
        }


        [RelayCommand]
        private void CallForServiceFromNavigationMenu()
        {
            if (ServiceCalled) return;
            CallForServicePanelIsOpen = true;
            ControlPanelVisible = true;
        }

        [RelayCommand]
        private void ServiceArrivalFromNavigationMenu()
        {
            if (ServiceArrival) return;
            ServiceArrivalPanelIsOpen = true;
            ControlPanelVisible = true;
        }

        [RelayCommand]
        private void WarrningNoticeStatusPanelPressed()
        {
            WarrningPanelIsOpen = false;
            ControlPanelVisible = false;
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
        private void OptionsButtonPressed()
        {
            OptionsPanelIsOpen = true;
            VacuumPanelIsOpen = true;
        }

        [RelayCommand]
        private void InfoButtonPressed()
        {
            InfoPanelIsOpen = true;
            ControlPanelVisible = true;
        }

        [RelayCommand]
        private void LoginButtonPressed()
        {
            if (EnteredPassword == "PT_9418")
            {
                IsPasswordProtected = true;
                EnteredPassword = "";
            }
            else
            {
                IsPasswordProtected = false;
            }
        }
        [RelayCommand(CanExecute = nameof(CanDeleteTheNumberStationAndGnv))]
        private void ClearButtonPressed()
        {
            NumberStation = "";
            VinHeatPump = "";
            _timerForVacuum.Stop();
            RemainingVacuumTime = TimeSpan.FromMinutes(5);
            VacuumProgressBarIsVisible = false;
            ProgressBarValue = 100;
            if (!HasErrors)
                ClearErrors();
            FocusRequested?.Invoke(this, "NumberStationTextBox");
        }

        [RelayCommand]
        private async Task GetLookupButtonPressed()
        {
            var getLookup = new GetLookup(this);
            await getLookup.GetLookupDefinitionsAsync();
        }

        [RelayCommand]
        private void ExitPressed()
        {
            Log.CloseAndFlush();
            System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        [RelayCommand]
        private void MinimizeApplication()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.WindowState = WindowState.Minimized;
                EnteredPassword = "";
                IsPasswordProtected = false;
            }
        }

        [RelayCommand]
        private void OpenFilesButtonPressed()
        {
            string resourcePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");
            Process.Start("explorer.exe", resourcePath);
            EnteredPassword = "";
            IsPasswordProtected = false;
            this.MinimizeApplication();
        }

        [RelayCommand]
        private void CreateScheduleForLogging()
        {
            if (isLoginToApp)
            {
                _timerScheduleForLogging = new DispatcherTimer();
                _timerScheduleForLogging.Interval = TimeSpan.FromMinutes(1);
                _timerScheduleForLogging.Tick += TimerScheduleLoggin_Tick;
                _timerScheduleForLogging.Start();
                LoginPanelIsOpen = true;
            }
            else
            {
                _timerScheduleForLogging = null;
                ControlPanelVisible = false;
            }
            EnteredPassword = "";
            IsPasswordProtected = false;
            OptionsPanelIsOpen = false;
        }

        private async void TimerTick(object sender, EventArgs e)
        {
            RemainingVacuumTime = RemainingVacuumTime.Subtract(TimeSpan.FromSeconds(1));

            ProgressBarValue = (RemainingVacuumTime.TotalSeconds / (appConfig.RemainingVacuumTimeDefault * 60)) * 100;

            try
            {
                List<double> pumpValues = await _messageFromPlc.ReadPumpDataFromPlc();
                PumpFourValue = Math.Round(pumpValues[2], 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd: {ex.Message}");
            }

            if (RemainingVacuumTime <= TimeSpan.Zero)
            {
                if (PumpFourValue >= 2.7)
                {
                    var box = MessageBoxManager.GetMessageBoxCustom(
                        new MessageBoxCustomParams
                        {
                            ButtonDefinitions = new List<ButtonDefinition>
                            {
                                new ButtonDefinition { Name = "OK", },
                            },
                            ContentTitle = "NOK",
                            ContentMessage = "Wartość ciśnienia jest powyżej 2,7 mBar.",
                            Icon = Icon.Error,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false,
                            MaxWidth = 500,
                            MaxHeight = 800,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            ShowInCenter = true,
                            Topmost = true
                        });
                    box.ShowAsync();
                }
                _timerForVacuum.Stop();
                ProgressBarValue = 100;
                RemainingVacuumTime = TimeSpan.FromMinutes(appConfig.RemainingVacuumTimeDefault);
                VacuumPanelIsOpen = false;
                VacuumProgressBarIsVisible = false;
            }
        }

        private void TimerScheduleLoggin_Tick(object? sender, EventArgs e)
        {
            int[] targetTimesInMinutes = { 5 * 60 + 30, 13 * 60 + 30, 21 * 60 + 30 }; // 5:30, 13:30, 21:30

            TimeSpan currentTimeOfDay = DateTime.Now.TimeOfDay;
            CurrentTime = currentTimeOfDay;

            if (Array.Exists(targetTimesInMinutes,
                    targetTime => currentTimeOfDay.TotalMinutes >= targetTime &&
                                  currentTimeOfDay.TotalMinutes < targetTime + 1))
            {
                LoginPanelIsOpen = true;
                ControlPanelVisible = true;
                if (LoginLabel != null)
                {
                    MessageInformationToSplunk message = new MessageInformationToSplunk();
                    message.Name = "S11.LogoutFromApp";
                    message.Status = "Logout";
                    //message.Value = "true";
                    //message.OperatorName = LoginLabel ?? null;
                    SendMessageToSplunk(message);
                    LoginLabel = null;
                }
            }
            else if (IsLoginToApp)
            {
                if (LoginLabel == null)
                {
                    LoginPanelIsOpen = true;
                    ControlPanelVisible = true;
                }
            }
        }

        [RelayCommand]
        private void OperatorLoginButtonPressed()
        {
            if (IsValidLogin(EnteredLogin))
            {
                LoginLabel = EnteredLogin.ToUpper();
                EnteredLogin = "";
                LoginPanelIsOpen = false;
                ControlPanelVisible = false;

                MessageInformationToSplunk message = new MessageInformationToSplunk();
                message.Name = "S11.LoginToApp";
                message.Status = "Login";
                //message.Value = "true";
                //message.OperatorName = LoginLabel;
                SendMessageToSplunk(message);
            }
            else
            {
                LoginPanelIsOpen = true;
            }
        }

        private bool IsValidLogin(string login)
        {
            if (string.IsNullOrEmpty(login) || login.Length < 2)
                return false;

            foreach (char c in login)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }

        [RelayCommand]
        private void OperatorLogoutButtonPressed()
        {
            EnteredLogin = "";
            LoginPanelIsOpen = false;
            ControlPanelVisible = false;

            MessageInformationToSplunk message = new MessageInformationToSplunk();
            message.Name = "S11.LogoutFromApp";
            message.Status = "Logout";
            //message.Value = "true";
            //message.OperatorName = LoginLabel ?? null;
            SendMessageToSplunk(message);
            LoginLabel = null;
        }

        [RelayCommand]
        private async void ScreenShotButtonPressed()
        {
            string folderPath = @"C:\screen\"; // Pełna ścieżka do folderu
            string filePath = null;

            // Sprawdź, czy folder istnieje, jeśli nie, to go utwórz
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            ScreenCapturer.StartCapture((Bitmap bitmap) =>
            {
                string fileName = $"SAP_ERROR_{DateTime.Now:yyyy_MM_dd_HH.mm.ss}.png";
                filePath = Path.Combine(folderPath, fileName);

                // Sprawdź ilość plików w folderze
                string[] existingFiles = Directory.GetFiles(folderPath, "SAP_ERROR_*.png");

                if (existingFiles.Length >= 10)
                {
                    // Jeżeli jest więcej niż 10 plików, usuń najstarszy
                    string oldestFile = existingFiles.OrderBy(f => new FileInfo(f).CreationTime).First();
                    File.Delete(oldestFile);
                }

                // Zapisz bieżące zrzut ekranu jako plik PNG
                bitmap.Save(filePath, ImageFormat.Png);

                ScreenCapturer.StopCapture();
                SendImageToDiscordWebhook(filePath);
            });

            var box = MessageBoxManager.GetMessageBoxCustom(
                new MessageBoxCustomParams
                {
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "OK", },
                    },
                    ContentTitle = "WYKONANO SCREEN",
                    ContentMessage = $"Wykonano screen z błędem który jest w : {folderPath} ",
                    Icon = Icon.Folder,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    MaxWidth = 500,
                    MaxHeight = 800,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInCenter = true,
                    Topmost = true
                });

            var result = await box.ShowAsync();
        }

        private async void SendImageToDiscordWebhook(string filePath)
        {
            MessageInformationToSplunk message = new MessageInformationToSplunk();
            message.Name = "S11.OperatorNotification";
            message.Status = "screenshot";
            message.Reason = filePath;
            SendMessageToSplunk(message);
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            // Get th channel configuration data
            var deviceInfoPanel = await mStatusInterfaceService.GetConfigHardwareAsync();
            var statusPanel = await mStatusInterfaceService.GetDowntimePanelAsync();
            var panelActionMapping = await mStatusInterfaceService.GetDowntimePanelActionsAsync();
            var settingStatusPanel = await mStatusInterfaceService.GetSettingPanelAsync();
            var maintenanceStatusPanel = await mStatusInterfaceService.GetMaintenancePanelAsync();
            var logisticStatusPanel = await mStatusInterfaceService.GetLogisticPanelAsync();
            var productionIssuesStatusPanel = await mStatusInterfaceService.GetProductionIssuesPanelAsync();
            var reasonDowntimeStatusPanel = await mStatusInterfaceService.GetReasonDowntimeMechanicalPanelAsync();
            var splunkStatusPanel = await mStatusInterfaceService.GetSplunkPanelAsync();
            var callForServicePanel = await mStatusInterfaceService.GetCallForServicePanelAsync();
            var serviceArrivalPanel = await mStatusInterfaceService.GetServiceArrivalPanelAsync();
            var downtimeReasonElectricPanel = await mStatusInterfaceService.GetDowntimeReasonElectricPanelAsync();
            var downtimeReasonLiderPanel = await mStatusInterfaceService.GetDowntimeReasonLiderPanelAsync();
            var downtimeReasonKptjPanel = await mStatusInterfaceService.GetDowntimeReasonKptjPanelAsync();
            var downtimeReasonPlatePanel = await mStatusInterfaceService.GetDowntimeReasonPlatePanelAsync();
            var downtimeReasonBindownicaPanel = await mStatusInterfaceService.GetDowntimeReasonBindownicaPanelAsync();
            var downtimeReasonSC200Panel = await mStatusInterfaceService.GetDowntimeReasonSC200PanelAsync();
            var downtimeReasonZebraPanel = await mStatusInterfaceService.GetDowntimeReasonZebraPanelAsync();
            var downtimeReasonWtryskarkaPanel = await mStatusInterfaceService.GetDowntimeReasonWtryskarkaPanelAsync();
            var downtimeReasonBradyPanel = await mStatusInterfaceService.GetDowntimeReasonBradyPanelAsync();
            var downtimeReasonWiazarkaPanel = await mStatusInterfaceService.GetDowntimeReasonWiazarkaPanelAsync();
            var downtimeReasonKomaxPanel = await mStatusInterfaceService.GetDowntimeReasonKomaxPanelAsync();
            var downtimeReasonURPanel = await mStatusInterfaceService.GetDowntimeReasonURPanelAsync();
            var downtimeReasonZFPanel = await mStatusInterfaceService.GetDowntimeReasonZFPanelAsync();
            var downtimeReasonTesterPanel = await mStatusInterfaceService.GetDowntimeReasonTesterPanelAsync();
            var downtimeReasonDozownikPanel = await mStatusInterfaceService.GetDowntimeReasonDozownikPanelAsync();
            var downtimeReasonWalcarkaPanel = await mStatusInterfaceService.GetDowntimeReasonWalcarkaPanelAsync();
            var downtimeReasonTesterWodnyPanel = await mStatusInterfaceService.GetDowntimeReasonTesterWodnyPanelAsync();
            var downtimeReasonLumbergPanel = await mStatusInterfaceService.GetDowntimeReasonLumbergPanelAsync();


            // Create a grouping from the flat data
            DeviceInfoPanel = new ObservableCollection<ConfigHardwareItem>(deviceInfoPanel);

            StatusPanel =
                new ObservableGroupedCollection<string, DowntimePanelItem>(
                    statusPanel.GroupBy(item => item.Status));

            RowForDowntimePanel = LoadSizeOfGrid(statusPanel.Count);

            PanelActionMapping = panelActionMapping.ToDictionary(pa => pa.Status, pa => pa.PanelName);

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

            ProductionIssuesStatusPanel =
                new ObservableGroupedCollection<string, ProductionIssuesPanelItem>(
                    productionIssuesStatusPanel.GroupBy(item => item.Name));

            ReasonDowntimeMechanicalPanel =
                new ObservableGroupedCollection<string, ReasonDowntimeMechanicalPanelItem>(
                    reasonDowntimeStatusPanel.GroupBy(item => item.Name));

            RowForReasonDowntimeMechanicalPanel = LoadSizeOfGrid(reasonDowntimeStatusPanel.Count);

            DowntimeReasonElectricPanel =
                new ObservableGroupedCollection<string, DowntimeReasonElectricPanelItem>(
                    downtimeReasonElectricPanel.GroupBy(item => item.Name));

            RowForDowntimeReasonElectricPanel = LoadSizeOfGrid(downtimeReasonElectricPanel.Count);

            DowntimeReasonLiderPanel =
                new ObservableGroupedCollection<string, DowntimeReasonLiderPanelItem>(
                    downtimeReasonLiderPanel.GroupBy(item => item.Name));

            RowForDowntimeReasonLiderPanel = LoadSizeOfGrid(DowntimeReasonLiderPanel.Count);

            DowntimeReasonKptjPanel =
                new ObservableGroupedCollection<string, DowntimeReasonKptjPanelItem>(
                    downtimeReasonKptjPanel.GroupBy(item => item.Name));

            RowForDowntimeReasonKptjPanel = LoadSizeOfGrid(downtimeReasonKptjPanel.Count);

            DowntimeReasonPlatePanel =
                new ObservableGroupedCollection<string, DowntimeReasonPlatePanelItem>(
                    downtimeReasonPlatePanel.GroupBy(item => item.Name));

            RowForDowntimeReasonPlatePanel = LoadSizeOfGrid(downtimeReasonPlatePanel.Count);

            SplunkPanel =
                new ObservableGroupedCollection<string, SplunkPanelItem>(
                    splunkStatusPanel.GroupBy(item => item.Group));

            CallForServicePanel =
                new ObservableGroupedCollection<string, CallForServicePanelItem>(
                callForServicePanel.GroupBy(item => item.Value));

            RowForCallForServicePanel = LoadSizeOfGrid(callForServicePanel.Count);

            ServiceArrivalPanel =
                new ObservableGroupedCollection<string, ServiceArrivalPanelItem>(
                    serviceArrivalPanel.GroupBy(item => item.Name));

            RowForServiceArrivalPanel = LoadSizeOfGrid(serviceArrivalPanel.Count);

            _allCallForServicePanelData = callForServicePanel;

            //new
            DowntimeReasonBindownicaPanel =
                new ObservableGroupedCollection<string, DowntimeReasonBindownicaPanelItem>(
                downtimeReasonBindownicaPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonBindownicaPanel = LoadSizeOfGrid(downtimeReasonBindownicaPanel.Count);

            DowntimeReasonSC200Panel =
                new ObservableGroupedCollection<string, DowntimeReasonSC200PanelItem>(
                downtimeReasonSC200Panel.GroupBy(item => item.Name));
            RowForDowntimeReasonSC200Panel = LoadSizeOfGrid(downtimeReasonSC200Panel.Count);

            DowntimeReasonZebraPanel =
                new ObservableGroupedCollection<string, DowntimeReasonZebraPanelItem>(
                downtimeReasonZebraPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonZebraPanel = LoadSizeOfGrid(downtimeReasonZebraPanel.Count);

            DowntimeReasonWtryskarkaPanel =
                new ObservableGroupedCollection<string, DowntimeReasonWtryskarkaPanelItem>(
                downtimeReasonWtryskarkaPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonWtryskarkaPanel = LoadSizeOfGrid(downtimeReasonWtryskarkaPanel.Count);

            DowntimeReasonBradyPanel =
                new ObservableGroupedCollection<string, DowntimeReasonBradyPanelItem>(
                downtimeReasonBradyPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonBradyPanel = LoadSizeOfGrid(downtimeReasonBradyPanel.Count);

            DowntimeReasonWiazarkaPanel =
                new ObservableGroupedCollection<string, DowntimeReasonWiazarkaPanelItem>(
                downtimeReasonWiazarkaPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonWiazarkaPanel = LoadSizeOfGrid(downtimeReasonWiazarkaPanel.Count);

            DowntimeReasonKomaxPanel =
                new ObservableGroupedCollection<string, DowntimeReasonKomaxPanelItem>(
                downtimeReasonKomaxPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonKomaxPanel = LoadSizeOfGrid(downtimeReasonKomaxPanel.Count);

            DowntimeReasonURPanel =
                new ObservableGroupedCollection<string, DowntimeReasonURPanelItem>(
                downtimeReasonURPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonURPanel = LoadSizeOfGrid(downtimeReasonURPanel.Count);

            DowntimeReasonZFPanel =
                new ObservableGroupedCollection<string, DowntimeReasonZFPanelItem>(
                downtimeReasonZFPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonZFPanel = LoadSizeOfGrid(downtimeReasonZFPanel.Count);

            DowntimeReasonTesterPanel =
                new ObservableGroupedCollection<string, DowntimeReasonTesterPanelItem>(
                downtimeReasonTesterPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonTesterPanel = LoadSizeOfGrid(downtimeReasonTesterPanel.Count);

            DowntimeReasonDozownikPanel =
                new ObservableGroupedCollection<string, DowntimeReasonDozownikPanelItem>(
                downtimeReasonDozownikPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonDozownikPanel = LoadSizeOfGrid(downtimeReasonDozownikPanel.Count);

            DowntimeReasonWalcarkaPanel =
                new ObservableGroupedCollection<string, DowntimeReasonWalcarkaPanelItem>(
                downtimeReasonWalcarkaPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonWalcarkaPanel = LoadSizeOfGrid(downtimeReasonWalcarkaPanel.Count);

            DowntimeReasonTesterWodnyPanel =
                new ObservableGroupedCollection<string, DowntimeReasonTesterWodnyPanelItem>(
                downtimeReasonTesterWodnyPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonTesterWodnyPanel = LoadSizeOfGrid(downtimeReasonTesterWodnyPanel.Count);

            DowntimeReasonLumbergPanel =
                new ObservableGroupedCollection<string, DowntimeReasonLumbergPanelItem>(
                downtimeReasonLumbergPanel.GroupBy(item => item.Name));
            RowForDowntimeReasonLumbergPanel = LoadSizeOfGrid(downtimeReasonLumbergPanel.Count);
        }

        private int LoadSizeOfGrid(int numberOfElements)
        {
            int maxColumns = 3;

            int rows = (int)Math.Ceiling((double)numberOfElements / maxColumns);

            return rows;
        }

        public void ActualValueForWarrningNoticePopup(MachineStatus machineStatus)
        {
            WarrningPanelIsOpen = true;
            WarrningNoticeText = machineStatus.Status;
            WarrningNoticeColor = machineStatus.Color;
        }

        public void ChangePropertyButtonStatus(string colorButton, string textButton)
        {
            ActualStatusButtonIsVisible = true;
            ActualStatusButtonText = textButton;
            ActualStatusColor = colorButton;
            ControlPanelVisible = false;
        }

        /// <summary>
        /// Before the refactoring
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>

        public async Task SendMessageToSplunk<T>(T message)
        {
            var valueProperty = typeof(T).GetProperty("Value");
            var statusProperty = typeof(T).GetProperty("Status");
            var nameProperty = typeof(T).GetProperty("Name");
            var value = valueProperty != null ? (string)valueProperty.GetValue(message) : null;
            var status = statusProperty != null ? (string)statusProperty.GetValue(message) : null;
            var name = nameProperty != null ? (string)nameProperty.GetValue(message) : null;


            var splunkLogger = new GenericSplunkLogger<T>(this);
            await splunkLogger.LogAsync(message);
        }

        [RelayCommand(CanExecute = nameof(CanSendDataToPlc))]
        public async Task SendMessageToPlc()
        {
            _timerForVacuum.Stop();
            ProgressBarValue = 100;
            RemainingVacuumTime = TimeSpan.FromMinutes(appConfig.RemainingVacuumTimeDefault);
            //ValidateAllProperties();
            //if (HasErrors)
            //{
            //    ClearButtonPressed();
            //    return;
            //}
            DateTime currentTime = DateTime.Now;

            List<object> dataFirstSend = new List<object>
            {
                currentTime.ToString("yyyy-MM-dd HH:mm:ss"),
                NumberStation,
                VinHeatPump
            };

            GenericMessageToPlc messageToPlc = new GenericMessageToPlc();
            _timerForVacuum.Start();
            await messageToPlc.WriteDataToPlc(dataFirstSend);


            await Task.Delay(5000);

            NumberStation = "";
            VinHeatPump = "";

            currentTime = DateTime.Now;

            List<object> dataSecondSend = new List<object>
            {
                currentTime.ToString("yyyy-MM-dd HH:mm:ss"), // Formatuj czas
                NumberStation,
                VinHeatPump
            };

            await messageToPlc.WriteDataToPlc(dataSecondSend);

            NumberStation = "";
            VinHeatPump = "";
        }

        private void LoadPage(string url)
        {
            if (ActivePage is UCBrowser browser)
            {
                // Jeśli przeglądarka już istnieje, zmieniamy adres
                browser.ChangeBrowserAddress(url);
            }
            else
            {
                // Jeśli nie ma przeglądarki, tworzymy nową instancję
                ActivePage = new UCBrowser(url);
            }
        }
        public void LoadPageManualViSyncMaster()
        {
            InfoPanelIsOpen = false;
            ControlPanelVisible = false;

            LoadPage("file:///C:/ViSM/ConfigFiles/Instrukcja%20ViSyncMaster.pdf");
        }

        public void LoadPageSap()
        {
            LoadPage(sapUrl);
        }

        public void LoadPageSplunk(string link)
        {
            LoadPage(link);
        }

        public void LoadPageAdaptronic()
        {
            LoadPage(adaptronicUrl);
        }

        public void LoadPageGoogleDisk()
        {
            LoadPage(googleDiskUrl);
        }

        public void LoadPageInstruction()
        {
            LoadPage(googleInstructionUrl);
        }

        public void LoadPageTargetPlan()
        {
            LoadPage(googleTargetPlanUrl);
        }

        [RelayCommand]
        private void LoadStatusTableOfMachine()
        {
            ActivePage = new MachineStatusTableView();
            (ActivePage as MachineStatusTableView)?.SetDataContext(MachineStatuses);
        }

        [RelayCommand]
        private void LoadStatusTableOfResult()
        {
            ActivePage = _resultTableView;
        }
        [RelayCommand]
        private void LoadFormFirstPart()
        {
            ActivePage = _firstPartView;
        }


        public void OpenSerialPort()
        {
            _serialPortListener = new SerialPortListener();
            _serialPortListener.FrameReceived += OnFrameReceived;
            StartListeningAsync();
        }


        public void LoginPanelOpen()
        {
            LoginPanelIsOpen = true;
        }

        private void KeepAlive(object state)
        {
            MessageInformationToSplunk message = new MessageInformationToSplunk();
            message.Name = "S11.KeepAlive";
            message.Status = "live";
            //message.Value = "true";
            //message.OperatorName = LoginLabel ?? null;
            SendMessageToSplunk(message);
        }

        private async Task ReSendMessageToSplunk(object state)
        {
            //var piece = new Rs232Data
            //{
            //    ProductName = "7976081 2/90 V.01 NceS",
            //    OperatorId = "smbl",
            //    TestingPassed = "true",
            //    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            //};
            //var batch = new List<Rs232Data> { piece };
            //await _machineStatusService.ReportBatchPartQuality(batch);

            Ssid = _wifiParameters.FetchWifiName();
            if (appConfig.AppMode == "VRSKT")
            {
                var messagePgToSplunk = _splunkMessageHandler.PreparingPgMessageToSplunk(MachineStatuses, _machineStatusCounter);
                await _machineStatusService.SendPgMessage((MessagePgToSplunk)messagePgToSplunk);
                await SendMessageToSplunk(messagePgToSplunk);
            }

            if (MachineStatuses != null && MachineStatuses.Any())
            {
                // Tworzymy kopię listy MachineStatuses
                var machineStatusesCopy = new List<MachineStatus>(MachineStatuses.Select(status => status.DeepCopy()));
                foreach (var machineStatus in machineStatusesCopy)
                {
                    await _machineStatusService.ReSendMessageToSplunk(machineStatus);
                }
            }
        }

        private async void StatusPingService(object sender, bool isPing)
        {
            await SendPingStatusToSplunk(isPing);
            await Task.Delay(30 * 1000);
            await SendPingStatusToSplunk(false);
        }

        private async Task SendPingStatusToSplunk(bool isPing)
        {
            //_messageToSplunkConnected.Value = isPing ? "true" : "false";
            await SendMessageToSplunk(_messageToSplunkConnected);
        }

        /// <summary>
        /// Asynchronous method to start listening to the COM port in a separate thread.
        /// </summary>
        /// 

        //private Rs232TestSimulator _testSimulator;

        //private void StartFakeTest()
        //{
        //   _testSimulator = new Rs232TestSimulator(_rs232Processor);
        //    _testSimulator.Start();
        //}

        private async void StartListeningAsync()
        {
            await Task.Run(() => _serialPortListener.StartListening(appConfig.ComNumber));
        }
        private string _previousProducingState = string.Empty;

        private async void OnFrameReceived(object sender, Rs232Data testData)
        {
            if (testData == null) return;

            testData.Producing = testData.Producing?.ToLower();
            testData.TestingPassed = testData.TestingPassed?.ToLower();
            testData.TestingFailed = testData.TestingFailed?.ToLower();

            if (BarOnTopApp != $"VRS  /  {testData.ST}")
                BarOnTopApp = $"VRS  /  {testData.ST}";

            if (LoginLabel != testData.Operator)
                LoginLabel = testData.Operator;

            Log.Information("Frame received → Producing: {Producing}, Passed: {Passed}, Failed: {Failed}, Product: {Product}, Operator: {Operator}",
                testData.Producing, testData.TestingPassed, testData.TestingFailed, testData.ProductName, testData.OperatorId);

            // Nowa logika – delegujemy analizę danych do Rs232DataProcessor
            _rs232Processor.Process(testData);
        }

        private void OnProducingStarted(object sender, Rs232Data data)
        {
            Log.Information("Produkcja rozpoczęta: " + data.ProductName);
            _machineStatusCounter = data.Producing == "true" ? 5 : 6;
            // Ustaw wiadomość na podstawie licznika
            _messageToSplunkPg.SetByCounter(_machineStatusCounter);
            ReSendMessageToSplunk(_messageToSplunkPg);
        }

        private void OnProducingEnded(object sender, Rs232Data data)
        {
            Log.Information("Produkcja zakończona: " + data.ProductName);
            _machineStatusCounter = data.Producing == "false" ? 6 : 5;
            _messageToSplunkPg.SetByCounter(_machineStatusCounter);
            ReSendMessageToSplunk(_messageToSplunkPg);
            if (data.Device != null)
            {
                Log.Information("Sending full test data to Splunk: {Data}", data);
                SendMessageToSplunk(data);
            }
        }

        private void OnProductionMetricsReady(object? sender, ProductionMetrics e)
        {
            Log.Information("ProductionMetrics, sztuk:", e);
            SendMessageToSplunk(e);
        }

        private async void OnTestBatchReady(object sender, List<Rs232Data> batch)
        {
            Log.Information("Batch gotowy, sztuk: {Count}", batch.Count);

            await _machineStatusService.ReportBatchPartQuality(batch);
        }

        private void TimerCallback(object state)
        {
            // Wywołujemy metodę asynchroniczną w tle
            Task.Run(async () => await ReSendMessageToSplunk(state));
        }
        #endregion

        private async void LoadStatuses(object state)
        {
            TimeSpan currentTimeOfDay = DateTime.Now.TimeOfDay;
            CurrentTime = currentTimeOfDay;
            var loadedStatuses = await _repositoryMachineStatus.GetFromCache();

            // Synchronizacja kolekcji

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Synchronizacja elementów w liście
                MachineStatuses.Clear();
                foreach (var status in loadedStatuses)
                {
                    MachineStatuses.Add(status);
                }
            });
        }
        private async void LoadResultToTable()
        {
            var loadedResult = await _repositoryTestingResult.GetFromCacheTestResult();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // zamiast Clear()+Add:
                ResultTest.SyncWith(loadedResult, x => x.Id);
            });

            // to odświeży tabelę i wywoła dalsze filtrowanie
            ResultTableUpdate?.Invoke(this, EventArgs.Empty);
        }

        #region InitializeAppFunctions
        /// <summary>
        /// InitializeApp regarding from 
        /// </summary>
        /// 

        private async Task InitializeLogger()
        {
            string logDirectory = @"C:\ViSM\App\logs";

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,           // Nowy plik codziennie
                    retainedFileCountLimit: 7,                      // Przechowuj tylko 7 dni logów
                    fileSizeLimitBytes: 10 * 1024 * 1024,           // Limit 10 MB na plik
                    rollOnFileSizeLimit: true)                      // Twórz nowy plik, jeśli przekroczono rozmiar
                .CreateLogger();
        }

        private async Task InitializeAsync()
        {
            _repositoryMachineStatus.UpdateCacheAsync();
            var activeMachineStatuses = await _repositoryMachineStatus.GetFromCache();
            var resultTestFromDb = await _repositoryTestingResult.GetFromCache();
            MachineStatuses = new ObservableCollection<MachineStatus>(activeMachineStatuses ?? new List<MachineStatus>());
            ResultTest = new ObservableCollection<MachineStatus>(resultTestFromDb ?? new List<MachineStatus>());
        }

        private async Task InitializeAppFunctions()
        {

            sapUrl = appConfig.UrlSap;
            isLoginToApp = appConfig.LoginToApp;


            if (double.TryParse(appConfig.SpanForKeepAlive, out double keepAliveMinutes))
            {
                _timer = new Timer(KeepAlive, null, TimeSpan.Zero, TimeSpan.FromMinutes(keepAliveMinutes));
            }

            _timerForReSendMassage = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
            _timerForLoadStatuses = new Timer(LoadStatuses, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            _wifiParameters = new WifiParameters();
            _anyDeskParameters = new AnyDeskParameters();
            _anyDeskId = _anyDeskParameters.FetchAnyDeskId();  

            // Sprawdzenie trybu z pliku konfiguracyjnego
            if (appConfig.AppMode == "CUPP")
            {
                // Inicjalizacja funkcji związanych z trybem CUPP
                InitializeCUPPFunctions();
            }
            else if (appConfig.AppMode == "VRSKT")
            {
                // Inicjalizacja funkcji związanych z trybem CHPKT
                InitializeVRSKTFunctions();
            }
            else
            {
                // W przypadku braku rozpoznanego trybu, można ustawić wartości domyślne lub wyrzucić wyjątek
                await ErrorHandler.ShowMissingFileError("Nieznany tryb konfiguracyjny.");
                throw new InvalidOperationException("Nieznany tryb konfiguracyjny.");
            }
        }

        private void InitializeCUPPFunctions()
        {
            adaptronicUrl = appConfig.UrlAdaptronic;
            googleDiskUrl = appConfig.UrlDiscGoogle;
            googleInstructionUrl = appConfig.UrlInstruction;
            googleTargetPlanUrl = appConfig.UrlTargetPlan;
            EnableFuction("CUPP");
            LoadPageSap();
            CreateScheduleForLogging();
        }

        private void InitializeVRSKTFunctions()
        {
            _messageToSplunkFailed = new TestingFailedMessage();
            _messageToSplunkPassed = new TestingPassedMessage();
            _messageToSplunkConnected = new ConnectedMessage();
            _messageToSplunkPg = new MessagePgToSplunk();
            _resultTableView = new ResultTableView();
            _firstPartView = new FormFirstPartView(_machineStatusService);
            _resultTableView.SetDataContext(ResultTest, this, _machineStatusService);
            // Inicjalizacja SerialPortListener tylko raz
            _serialPortListener = new SerialPortListener();
            _rs232Processor = new Rs232DataProcessor();
            _rs232Processor.ProducingStarted += OnProducingStarted;
            _rs232Processor.ProducingEnded += OnProducingEnded;
            _rs232Processor.TestBatchReady += OnTestBatchReady;
            _rs232Processor.ProductionMetricsReady += OnProductionMetricsReady;
            _serialPortListener.FrameReceived += OnFrameReceived;
            _machineStatusService.TableResultTestUpdate += LoadResultToTable;
            adaptronicUrl = appConfig.UrlAdaptronic;
            googleDiskUrl = appConfig.UrlDiscGoogle;
            googleInstructionUrl = appConfig.UrlInstruction;
            googleTargetPlanUrl = appConfig.UrlTargetPlan;


            EnableFuction("VRSKT");

            pingService = new PingService(appConfig.IpTesterKt, null);
            pingService.PingCompleted += StatusPingService;
            pingService.Start();

            // Start nasłuchiwania portu w tle
            StartListeningAsync();
            LoadPageAdaptronic();
            LoadResultToTable();
            //StartFakeTest();
        }

        private void EnableFuction(string appMode)
        {
            switch (appMode)
            {
                case "VRSKT":
                    OpenSerialPortButtonIsVisible = true;
                    AdaptronicButtonIsVisible = true;
                    GoogleDriveButtonIsVisible = true;
                    InstructionButtonIsVisible = true;
                    TargetPlanButtonIsVisible = true;
                    UserButtonIsVisible = false;
                    break;

                case "CUPP":
                    OpenSerialPortButtonIsVisible = false;
                    AdaptronicButtonIsVisible = false;
                    GoogleDriveButtonIsVisible = false;
                    InstructionButtonIsVisible = false;
                    TargetPlanButtonIsVisible = false;
                    UserButtonIsVisible = true;
                    break;

                default:
                    OpenSerialPortButtonIsVisible = true;
                    AdaptronicButtonIsVisible = true;
                    GoogleDriveButtonIsVisible = true;
                    InstructionButtonIsVisible = true;
                    TargetPlanButtonIsVisible = true;
                    UserButtonIsVisible = true;
                    break;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default contructor
        /// </summary>
        /// <param name="statusInterfaceService">The status interface service</param>
        /// 
        private SerialPortListener _serialPortListener;
        private MqttMessageSender _mqttSender;

        public MainWindowViewModel(IStatusInterfaceService statusInterfaceService)
        {
            InitializeLogger();
            mStatusInterfaceService = statusInterfaceService;
            _pendingMachineStatus = new MachineStatus();
            _sharedDataService = new SharedDataService();
            appConfig = _sharedDataService.AppConfig ?? new AppConfigData();
            mqttConfig = _sharedDataService.ConfigMqtt ?? new ConfigMqtt();
            _database = new SQLiteDatabase(@"C:\ViSM\Database\databaseViSM.db");
            _database.CreateTableIfNotExists<MachineStatus>("MachineStatus");
            _database.CreateTableIfNotExists<MachineStatus>("MachineStatusQueue");
            _database.CreateTableIfNotExists<MachineStatus>("TestingResultQueue");
            _database.CreateTableIfNotExists<MachineStatus>("TestingResult");
            _database.CreateTableIfNotExists<ProductionEfficiency>("ProductionEfficiency");
            _database.CreateTableIfNotExists<FirstPartModel>("FirstPartData");
            _repositoryMachineStatus = new GenericRepository<MachineStatus>(_database, "MachineStatus");
            _repositoryMachineStatusQueue = new GenericRepository<MachineStatus>(_database, "MachineStatusQueue");
            _repositoryTestingResultQueue = new GenericRepository<MachineStatus>(_database, "TestingResultQueue");
            _repositoryTestingResult = new GenericRepository<MachineStatus>(_database, "TestingResult");
            _repositoryProductionEfficiency = new GenericRepository<ProductionEfficiency>(_database, "ProductionEfficiency");
            _repositoryFirstPartData = new GenericRepository<FirstPartModel>(_database, "FirstPartData");
            // Inicjalizacja widoku, który będzie używany przez DataContext
            _messageSender = new MessageSender(this); // Na początku brak połączenia   
            _messageQueue = new MessageQueue(_repositoryMachineStatusQueue, _repositoryTestingResultQueue, _repositoryProductionEfficiency, _repositoryFirstPartData, _messageSender);
            _splunkMessageHandler = new SplunkMessageHandler();
            _mqttSender = new MqttMessageSender(mqttConfig.brokerHost, mqttConfig.brokerPort, appConfig.Source, mqttConfig.username, mqttConfig.password);

            _machineStatusService = new MachineStatusService(_repositoryMachineStatus, _repositoryMachineStatusQueue,
                _repositoryTestingResultQueue, _repositoryTestingResult, _repositoryProductionEfficiency, _repositoryFirstPartData, _messageSender, _messageQueue, _database, _mqttSender);


            LoadStatuses(this);
            InitializeAsync();



            if (appConfig.AppMode != null)
                InitializeAppFunctions();

            CurrentTime = DateTime.Now.TimeOfDay;
            _timerForVacuum = new DispatcherTimer();
            _timerForVacuum.Interval = TimeSpan.FromSeconds(1);
            _timerForVacuum.Tick += TimerTick;
            BarOnTopApp = !string.IsNullOrEmpty(appConfig.Line) && !string.IsNullOrEmpty(appConfig.WorkplaceName)
                ? $"{appConfig.Line}  /  {appConfig.WorkplaceName}"
                : "Brak danych / Brak danych";
            _remainingVacuumTime = TimeSpan.FromMinutes(appConfig.RemainingVacuumTimeDefault);
            VacuumButtonIsVisible = appConfig.VacuumPanelAvailable;
            VacuumPanelAvailable = appConfig.VacuumPanelAvailable;
            _messageFromPlc = new GenericMessageFromPlc();
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
