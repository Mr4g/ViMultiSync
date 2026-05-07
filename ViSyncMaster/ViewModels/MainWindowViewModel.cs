using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using ScreenCapturerNS;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.AuxiliaryClasses;
using ViSyncMaster.DataModel;
using ViSyncMaster.DeepCopy;
using ViSyncMaster.Entitys;
using ViSyncMaster.Handlers;
using ViSyncMaster.Heleprs;
using ViSyncMaster.OPCUA;
using ViSyncMaster.Repositories;
using ViSyncMaster.Services;
using ViSyncMaster.Services.Test;
using ViSyncMaster.SystemParameters;
using ViSyncMaster.Views;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Bitmap = System.Drawing.Bitmap;
using Icon = MsBox.Avalonia.Enums.Icon;
using Path = System.IO.Path;
using Timer = System.Threading.Timer;


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
        private Rs232Data? _lastRs232Data;
        private MessagePgToSplunk _messageToSplunkPg;
        private int _machineStatusCounter = 6;
        private GenericSplunkLogger<IEntity> _splunkLogger;
        private WifiParameters _wifiParameters;
        private AnyDeskParameters _anyDeskParameters;   

        private DispatcherTimer _timerScheduleForLogging;
        private TaskCompletionSource<bool> _dataIsSendingToSplunkCompletionSource = new TaskCompletionSource<bool>();

        private PingService pingService;
        private MachineStatus _pendingMachineStatus;
        private OpcUaMultiWatchService? _opcUa;
        private bool _isOpcUaInitialized;


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
        private readonly Dictionary<string, List<string>> _vrsktQualityCategoryReasons = new()
        {
            ["Kabel"] = new() { "Uszkodzenia mechaniczne", "Długości", "Materiał" },
            ["Zacisk"] = new() { "Uszkodzenia mechaniczne", "Materiał", "Brak", "Pomiary", "Jakość" },
            ["Wtyczka"] = new() { "Uszkodzenia mechaniczne", "Materiał", "Brak", "Pomiary", "Jakość", "Montaż", "Nadruk" },
            ["Tulejka"] = new() { "Uszkodzenia mechaniczne", "Materiał", "Brak", "Montaż" },
            ["Binder"] = new() { "Uszkodzenia mechaniczne", "Odległość", "Materiał", "Brak", "Montaż" },
            ["Wtrysk"] = new() { "Uszkodzenia mechaniczne", "Odległość", "Materiał", "Brak", "Wady odlewu", "Pomiary" },
            ["Etykiety"] = new() { "Uszkodzenia mechaniczne", "Odległość", "Materiał", "Brak", "Nadruk" },
            ["Termokurcz"] = new() { "Uszkodzenia mechaniczne", "Długości", "Odległość", "Materiał", "Brak" },
            ["Sensory, komponenty"] = new() { "Uszkodzenia mechaniczne", "Materiał", "Odległość", "Brak", "Pomiary", "Jakość", "Montaż", "Nadruk", "Rezystancja" },
            ["Wady dostawców"] = new() { "Uszkodzenia mechaniczne", "Materiał", "Długości", "Brak", "Pomiary", "Jakość", "Montaż", "Nadruk", "Rezystancja" },
            ["Inne"] = new() { "Własny opis" }
        };

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
        private readonly GenericRepository<HourlyPlanMessage> _repositoryHourlyPlan;
        private readonly MessageQueue _messageQueue;
        private readonly MessageSender _messageSender;
        private readonly MachineStatusService _machineStatusService;
        private ResultTableView _resultTableView;
        private FormFirstPartView _firstPartView;
        private ScadaHostView? _scadaView;
        ///private readonly NotepadHostControl _notepadHost = new();

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
        [ObservableProperty]
        private bool _scadaButtonIsVisible;
        [ObservableProperty]
        private bool _qualityIssuesTabIsVisible;

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
        [ObservableProperty] private bool _vrsktQualityElementSelectionVisible;
        [ObservableProperty] private bool _vrsktQualityReasonSelectionVisible;
        [ObservableProperty] private bool _vrsktQualityFlowActive;
        [ObservableProperty] private bool _legacyProductionIssuesVisible = true;
        [ObservableProperty] private string _vrsktQualityConfirmationText = string.Empty;
        [ObservableProperty] private bool _vrsktQualityConfirmationVisible;
        [ObservableProperty] private bool _vrsktQualitySuccessOverlayVisible;
        [ObservableProperty] private string _vrsktQualityOverlayBackground = "#1E7D32";
        [ObservableProperty] private ObservableCollection<string> _vrsktQualityElementTypes = new();
        [ObservableProperty] private ObservableCollection<string> _vrsktQualityReasons = new();
        [ObservableProperty] private string? _selectedVrsktQualityElementType;
        [ObservableProperty] private string? _selectedVrsktQualityReason;
        [ObservableProperty] private string? _vrsktQualityCustomDescription;
        [ObservableProperty] private bool _vrsktQualityCustomDescriptionVisible;
        [ObservableProperty] private int _vrsktQualityQuantity = 1;

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
        private Control? _activePage;

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
        public void OpenProductionIssuesFlow()
        {
            if (string.Equals(appConfig.AppMode, "VRSKT", StringComparison.OrdinalIgnoreCase))
            {
                InitializeVrsktQualityFlow();
                ProductionIssuesPanelIsOpen = true;
                ControlPanelVisible = true;
                return;
            }
            ProductionIssuesPanelButtonPressed();
        }

        [RelayCommand]
        public void SelectVrsktQualityElementType(string elementType)
        {
            if (!string.Equals(appConfig.AppMode, "VRSKT", StringComparison.OrdinalIgnoreCase)) return;
            SelectedVrsktQualityElementType = elementType;
            VrsktQualityReasons = new ObservableCollection<string>(_vrsktQualityCategoryReasons.GetValueOrDefault(elementType, new List<string>()));
            VrsktQualityElementSelectionVisible = false;
            VrsktQualityReasonSelectionVisible = true;
            VrsktQualityCustomDescriptionVisible = false;
            VrsktQualityCustomDescription = string.Empty;
        }

        [RelayCommand]
        public void BackVrsktQualityToCategory()
        {
            if (!string.Equals(appConfig.AppMode, "VRSKT", StringComparison.OrdinalIgnoreCase)) return;
            VrsktQualityReasonSelectionVisible = false;
            VrsktQualityElementSelectionVisible = true;
            SelectedVrsktQualityReason = null;
            VrsktQualityCustomDescriptionVisible = false;
            VrsktQualityCustomDescription = string.Empty;
        }

        [RelayCommand]
        public void CancelVrsktQualityReport()
        {
            ProductionIssuesPanelIsOpen = false;
            ControlPanelVisible = false;
            ResetVrsktQualityFlowState();
        }

        [RelayCommand]
        public async Task SubmitVrsktQualityReason(string qualityReason)
        {
            if (!string.Equals(appConfig.AppMode, "VRSKT", StringComparison.OrdinalIgnoreCase)) return;
            SelectedVrsktQualityReason = qualityReason;
            VrsktQualityCustomDescriptionVisible = string.Equals(SelectedVrsktQualityElementType, "Inne", StringComparison.OrdinalIgnoreCase);
        }

        [RelayCommand]
        public async Task SendSelectedVrsktQualityReport()
        {
            if (!string.Equals(appConfig.AppMode, "VRSKT", StringComparison.OrdinalIgnoreCase)) return;
            if (VrsktQualityCustomDescriptionVisible)
            {
                return;
            }
            await SendVrsktQualityReportAsync();
        }

        [RelayCommand]
        public async Task SubmitVrsktQualityCustomDescription()
        {
            if (!string.Equals(appConfig.AppMode, "VRSKT", StringComparison.OrdinalIgnoreCase)) return;
            await SendVrsktQualityReportAsync();
        }

        private async Task SendVrsktQualityReportAsync()
        {
            var activeProductNumber = _lastRs232Data?.ProductName;
            if (string.IsNullOrWhiteSpace(activeProductNumber))
            {
                var resultHistory = await _repositoryTestingResult.GetFromCacheTestResult();
                activeProductNumber = resultHistory?
                    .Where(x => !string.IsNullOrWhiteSpace(x.ProductName))
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.ProductName)
                    .FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(activeProductNumber) ||
                string.IsNullOrWhiteSpace(SelectedVrsktQualityElementType) ||
                string.IsNullOrWhiteSpace(SelectedVrsktQualityReason))
            {
                var missingFields = new List<string>();

                if (string.IsNullOrWhiteSpace(activeProductNumber))
                {
                    missingFields.Add("numer aktualnie produkowanej sztuki (ActiveProductNumber)");
                }
                if (string.IsNullOrWhiteSpace(SelectedVrsktQualityElementType))
                {
                    missingFields.Add("typ elementu");
                }
                if (string.IsNullOrWhiteSpace(SelectedVrsktQualityReason))
                {
                    missingFields.Add("powód jakościowy");
                }

                var details = string.Join(Environment.NewLine, missingFields.Select(field => $"- {field}"));
                ShowMessageBox($"Brak wymaganych danych zgłoszenia jakościowego:{Environment.NewLine}{details}");
                return;
            }
            if (VrsktQualityCustomDescriptionVisible && string.IsNullOrWhiteSpace(VrsktQualityCustomDescription))
            {
                ShowMessageBox("Dla kategorii Inne wymagany jest własny opis.");
                return;
            }
            if (VrsktQualityQuantity <= 0)
            {
                ShowMessageBox("Liczba sztuk musi być większa od 0.");
                return;
            }

            var qualityReport = new QualityIssueReportMessage
            {
                AppMode = "VRSKT",
                ActiveProductNumber = NormalizeActiveProductNumber(activeProductNumber),
                ElementType = SelectedVrsktQualityElementType,
                QualityReason = SelectedVrsktQualityReason,
                CustomDescription = VrsktQualityCustomDescriptionVisible ? VrsktQualityCustomDescription : null,
                Quantity = VrsktQualityQuantity,
                EventType = "QualityIssueReported",
                ReportNature = "InformationalOnly_NoMachineOrProcessImpact",
                TimestampUtc = DateTime.UtcNow
            };

            VrsktQualityConfirmationText = "Wysyłanie zgłoszenia jakościowego...";
            VrsktQualityOverlayBackground = "#3A3F4B";
            VrsktQualityConfirmationVisible = true;
            VrsktQualitySuccessOverlayVisible = true;

            var sendTask = SendMessageToSplunk(qualityReport);
            var completedTask = await Task.WhenAny(sendTask, Task.Delay(TimeSpan.FromSeconds(5)));
            if (completedTask != sendTask)
            {
                VrsktQualityConfirmationText = "Błąd wysyłki: brak potwierdzenia do 5 sekund. Sprawdź połączenie i spróbuj ponownie.";
                VrsktQualityOverlayBackground = "#B3261E";
                await Task.Delay(TimeSpan.FromSeconds(3));
                VrsktQualitySuccessOverlayVisible = false;
                VrsktQualityConfirmationVisible = false;
                return;
            }

            await sendTask;
            VrsktQualityConfirmationText = $"Zgłoszenie jakościowe wysłane poprawnie dla produktu: {qualityReport.ActiveProductNumber}";
            VrsktQualityOverlayBackground = "#1E7D32";
            await Task.Delay(TimeSpan.FromSeconds(3));
            VrsktQualitySuccessOverlayVisible = false;
            VrsktQualityConfirmationVisible = false;
            ProductionIssuesPanelIsOpen = false;
            ControlPanelVisible = false;
            ResetVrsktQualityFlowState();
        }

        private static string NormalizeActiveProductNumber(string productNumber)
        {
            if (string.IsNullOrWhiteSpace(productNumber))
            {
                return string.Empty;
            }

            var digitsOnly = new string(productNumber.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length >= 7)
            {
                return digitsOnly[..7];
            }

            return productNumber.Trim();
        }

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
        private Task ReportMachineDowntime(MachineStatus item)
        {
            _pendingMachineStatus = item.DeepCopy();

            _ = AskIfLineStopsViaPopupAsync()
                .ContinueWith(t => HandleReportMachineDowntimeAsync(item, t.Result),
                    TaskScheduler.FromCurrentSynchronizationContext())
                .Unwrap();

            return Task.CompletedTask;
        }

        private async Task HandleReportMachineDowntimeAsync(MachineStatus item, bool isLineStopped)
        {
            IsLineStopped = isLineStopped;

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
                IsLineStopped = await AskIfLineStopsViaPopupAsync();

                await _machineStatusService.StartStatus(newStatus, IsLineStopped);
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
            await _machineStatusService.EndStatus(machineStatus, IsLineStopped); // Kończenie statusu bez powodu
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
                await _machineStatusService.StartStatus(pendingStatus, IsLineStopped);
                var messagePgToSplunk = _splunkMessageHandler.PreparingPgMessageToSplunk(MachineStatuses, pendingStatus, _machineStatusCounter);
                await _machineStatusService.SendPgMessage((MessagePgToSplunk)messagePgToSplunk);

            }
            if (panelItem.Name == "ServiceArrival")
            {
                pendingStatus.ServiceArrival = DateTime.Now;
                await _machineStatusService.UpdateStatus(pendingStatus, IsLineStopped);
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
            DisposeOpcUaService();
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

        [RelayCommand]
        public async Task LoadScadaSystemAsync()
        {
            await ScadaProcessManager.Instance.EnsureStartedAsync();
            if (ActivePage != _scadaView)
            {
                ActivePage = _scadaView;
            }
        }

        private void OnStandbyChanged(bool standby)
        {
            if (!standby)
            {
                Dispatcher.UIThread.Post(async () => await LoadScadaSystemAsync());
            }
        }

        private void OnTestOkChange(bool testOk)
        {
            //if (testOk)
            //{
            //    Dispatcher.UIThread.Post(LoadPageSap);
            //}
            //else
            //{
            //    Dispatcher.UIThread.Post(async () => await LoadScadaSystemAsync());
            //}
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

        private async void KeepAlive(object state)
        {
            MessageInformationToSplunk message = new MessageInformationToSplunk();
            message.Name = "S11.KeepAlive";
            message.Status = "live";
            //message.Value = "true";
            //message.OperatorName = LoginLabel ?? null;
            SendMessageToSplunk(message);
            var pgMessage = new MessagePgToSplunk();
            await _machineStatusService.SendPgMessage(pgMessage);
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
                    await _machineStatusService.ReSendMessageToSplunk(machineStatus, IsLineStopped);
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
            _lastRs232Data = testData;

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

        private void InitializeVrsktQualityFlow()
        {
            VrsktQualityElementTypes = new ObservableCollection<string>(_vrsktQualityCategoryReasons.Keys);
            VrsktQualityReasons = new ObservableCollection<string>();
            VrsktQualityElementSelectionVisible = true;
            VrsktQualityReasonSelectionVisible = false;
            VrsktQualityFlowActive = true;
            LegacyProductionIssuesVisible = false;
            SelectedVrsktQualityElementType = null;
            SelectedVrsktQualityReason = null;
            VrsktQualityCustomDescription = string.Empty;
            VrsktQualityCustomDescriptionVisible = false;
            VrsktQualityQuantity = 1;
            VrsktQualityConfirmationVisible = false;
            VrsktQualityConfirmationText = string.Empty;
            VrsktQualitySuccessOverlayVisible = false;
            VrsktQualityOverlayBackground = "#1E7D32";
        }

        private void ResetVrsktQualityFlowState()
        {
            VrsktQualityElementSelectionVisible = false;
            VrsktQualityReasonSelectionVisible = false;
            VrsktQualityFlowActive = false;
            LegacyProductionIssuesVisible = true;
            SelectedVrsktQualityElementType = null;
            SelectedVrsktQualityReason = null;
            VrsktQualityCustomDescription = string.Empty;
            VrsktQualityCustomDescriptionVisible = false;
            VrsktQualityQuantity = 1;
        }

        public void ResetVrsktQualityFlowAfterPopupDismiss()
        {
            if (!VrsktQualityFlowActive)
            {
                return;
            }

            ResetVrsktQualityFlowState();
            VrsktQualityConfirmationVisible = false;
            VrsktQualityConfirmationText = string.Empty;
            VrsktQualityOverlayBackground = "#1E7D32";
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
            var appMode = appConfig.AppMode?.Trim();

            if (!string.Equals(appMode, "ODUSCADA", StringComparison.OrdinalIgnoreCase))
            {
                DisposeOpcUaService();
            }

            if (string.Equals(appMode, "CUPP", StringComparison.OrdinalIgnoreCase))
            {
                // Inicjalizacja funkcji związanych z trybem CUPP
                InitializeCUPPFunctions();
            }
            else if (string.Equals(appMode, "VRSKT", StringComparison.OrdinalIgnoreCase))
            {
                // Inicjalizacja funkcji związanych z trybem CHPKT
                InitializeVRSKTFunctions();

            }
            else if (string.Equals(appMode, "ODUSCADA", StringComparison.OrdinalIgnoreCase))
            {
                // Inicjalizacja funkcji związanych z trybem CHPKT
                InitializeODUSCADAFunctions();
                _scadaView ??= new ScadaHostView();
                // Konfiguracja ścieżki do SCADY
                ScadaProcessManager.Instance.StartPath = @"C:\ViSM\SCADA\W1605 SCADA.lnk";
                ScadaProcessManager.Instance.WindowTitleMatch = "W1605 SCADA";

                await EnsureOpcUaInitializedAsync();
            }
            else
            {
                // W przypadku braku rozpoznanego trybu, można ustawić wartości domyślne lub wyrzucić wyjątek
                await ErrorHandler.ShowMissingFileError("Nieznany tryb konfiguracyjny.");
                throw new InvalidOperationException("Nieznany tryb konfiguracyjny.");
            }
        }

        private async Task EnsureOpcUaInitializedAsync()
        {
            if (_isOpcUaInitialized)
            {
                return;
            }

            _opcUa = new OpcUaMultiWatchService("opc.tcp://10.109.142.2:4840");
            _opcUa.BoolChanged += OnOpcUaBoolChanged;

            _opcUa.AddWatch(
                key: "Standby",
                nodeIdString: "ns=3;s=\"IOT_Furness_IV1673000332\".\"IOT_Data\".\"Current_Stage\".\"Standby\"",
                samplingIntervalMs: 200);

            _opcUa.AddWatch(
                key: "TEST_OK",
                nodeIdString: "ns=3;s=\"IOT_Furness_IV1673000332\".\"IOT_Data\".\"S7.TEST_OK\"",
                samplingIntervalMs: 200);

            try
            {
                await _opcUa.StartAsync(useSecurity: false, publishingIntervalMs: 500, defaultSamplingIntervalMs: 200);
                _isOpcUaInitialized = true;
            }
            catch
            {
                DisposeOpcUaService();
                throw;
            }
        }

        private void DisposeOpcUaService()
        {
            if (_opcUa != null)
            {
                _opcUa.BoolChanged -= OnOpcUaBoolChanged;
                _opcUa.Dispose();
                _opcUa = null;
                _isOpcUaInitialized = false;
            }
        }

        private void OnOpcUaBoolChanged(string key, bool val)
        {
            switch (key)
            {
                case "Standby":
                    OnStandbyChanged(val);
                    break;
                case "TEST_OK":
                    OnTestOkChange(val);
                    break;
                    // dopisuj kolejne w razie potrzeby
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

        private void InitializeODUSCADAFunctions()
        {
            EnableFuction("ODUSCADA");
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
                    ScadaButtonIsVisible = false;
                    QualityIssuesTabIsVisible = true;
                    break;

                case "CUPP":
                    OpenSerialPortButtonIsVisible = false;
                    AdaptronicButtonIsVisible = false;
                    GoogleDriveButtonIsVisible = false;
                    InstructionButtonIsVisible = false;
                    TargetPlanButtonIsVisible = false;
                    UserButtonIsVisible = true;
                    ScadaButtonIsVisible = false;
                    QualityIssuesTabIsVisible = false;
                    break;

                case "ODUSCADA":
                    OpenSerialPortButtonIsVisible = false;
                    AdaptronicButtonIsVisible = false;
                    GoogleDriveButtonIsVisible = false;
                    InstructionButtonIsVisible = false;
                    TargetPlanButtonIsVisible = false;
                    UserButtonIsVisible = true;
                    ScadaButtonIsVisible = true;
                    QualityIssuesTabIsVisible = false;
                    break;

                default:
                    OpenSerialPortButtonIsVisible = true;
                    AdaptronicButtonIsVisible = true;
                    GoogleDriveButtonIsVisible = true;
                    InstructionButtonIsVisible = true;
                    TargetPlanButtonIsVisible = true;
                    UserButtonIsVisible = true;
                    QualityIssuesTabIsVisible = false;
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

        // --- COMMAND (kliknięcie opcji) ---
        [RelayCommand]
        private void LineStopPanelOptionPressed(LineStopOption? option)
        {
            // 1) Najpierw rozwiąż TCS
            _lineStopTcs?.TrySetResult(option.Value);
            _lineStopTcs = null;

            // 2) Potem zamknij popup i overlay
            LineStopPanelIsOpen = false;
            ControlPanelVisible = false;
        }

        // --- HELPER: pokaż popup i poczekaj na odpowiedź ---
        private async Task<bool> AskIfLineStopsViaPopupAsync()
        {
            if (_lineStopTcs != null)
                return await _lineStopTcs.Task;

            _lineStopTcs = new TaskCompletionSource<bool>();

            LineStopPanelOptions = new ObservableCollection<LineStopOption>(new[]
            {
                new LineStopOption { Label = "TAK – zatrzymuje",      Value = true  },
                new LineStopOption { Label = "NIE – nie zatrzymuje",  Value = false },
                new LineStopOption { Label = "Exit", Value = false } 
            });

            ControlPanelVisible = true;
            LineStopPanelIsOpen = true;

            return await _lineStopTcs.Task;   // zawsze true/false
        }

        partial void OnLineStopPanelIsOpenChanged(bool value)
        {
            // Jeśli popup został zamknięty “z boku” (klik w tło / ESC),
            // domknij oczekujące TCS jako Anuluj (null), żeby kolejny raz mógł się otworzyć.
            if (!value && _lineStopTcs != null)
            {
                _lineStopTcs.TrySetResult(false);
                _lineStopTcs = null;
            }
        }

        // --- PROPERTIES ---
        [ObservableProperty] private bool _lineStopPanelIsOpen;
        [ObservableProperty] private ObservableCollection<LineStopOption> _lineStopPanelOptions = new();
        [ObservableProperty] private bool _isLineStopped;

        // do czekania na wybór użytkownika
        private TaskCompletionSource<bool>? _lineStopTcs;

        // Mała klasa opcji dla popupu
        public sealed class LineStopOption
        {
            public string Label { get; set; } = "";
            public bool Value { get; set; } // true = TAK, false = NIE, null = Anuluj
        }

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
            _database.CreateTableIfNotExists<HourlyPlanMessage>("HourlyPlanMessage");
            _repositoryMachineStatus = new GenericRepository<MachineStatus>(_database, "MachineStatus");
            _repositoryMachineStatusQueue = new GenericRepository<MachineStatus>(_database, "MachineStatusQueue");
            _repositoryTestingResultQueue = new GenericRepository<MachineStatus>(_database, "TestingResultQueue");
            _repositoryTestingResult = new GenericRepository<MachineStatus>(_database, "TestingResult");
            _repositoryProductionEfficiency = new GenericRepository<ProductionEfficiency>(_database, "ProductionEfficiency");
            _repositoryFirstPartData = new GenericRepository<FirstPartModel>(_database, "FirstPartData");
            _repositoryHourlyPlan = new GenericRepository<HourlyPlanMessage>(_database, "HourlyPlanMessage");
            // Inicjalizacja widoku, który będzie używany przez DataContext
            _messageSender = new MessageSender(this); // Na początku brak połączenia   
            _messageQueue = new MessageQueue(_repositoryMachineStatusQueue, _repositoryTestingResultQueue, _repositoryProductionEfficiency, _repositoryFirstPartData, _repositoryHourlyPlan, _messageSender); _splunkMessageHandler = new SplunkMessageHandler();
            _mqttSender = new MqttMessageSender(mqttConfig.brokerHost, mqttConfig.brokerPort, appConfig.Source, mqttConfig.username, mqttConfig.password);

            _machineStatusService = new MachineStatusService(_repositoryMachineStatus, _repositoryMachineStatusQueue,
               _repositoryTestingResultQueue, _repositoryTestingResult, _repositoryProductionEfficiency, _repositoryFirstPartData, _repositoryHourlyPlan, _messageSender, _messageQueue, _database, _mqttSender);

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
