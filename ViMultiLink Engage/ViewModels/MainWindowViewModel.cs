﻿using System;
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
using ViMultiSync.DataModel;
using ViMultiSync.Entitys;
using ViMultiSync.Repositories;
using ViMultiSync.Services;
using ViMultiSync.Views;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using ScreenCapturerNS;
using Bitmap = System.Drawing.Bitmap;
using Icon = MsBox.Avalonia.Enums.Icon;
using Path = System.IO.Path;
using System.Timers;
using Timer = System.Threading.Timer;


namespace ViMultiSync.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region Private Memebers

        private IStatusInterfaceService mStatusInterfaceService;
        private Dictionary<Type, object> repositories = new Dictionary<Type, object>();
        private readonly SharedDataService _sharedDataService;
        private readonly AppConfigData appConfig;
        private Timer _timer;

        private DispatcherTimer _timerScheduleForLogging;
        private TaskCompletionSource<bool> _dataIsSendingToSplunkCompletionSource = new TaskCompletionSource<bool>();


        string screenshotPath = "C:/zrzut_ekranu.png"; // Ścieżka, gdzie zostanie zapisany zrzut ekranu
        string imgurClientId = "0fe6e59673311dc"; // Zastąp wartością swojego Client ID zarejestrowanego na Imgur
        string imagePath = @"\screen\SAP_ERROR.png";
        private string sapUrl;

        #endregion
        private bool sentMessageWithTrue = false;
        IEntity _lastMessage;

        #region Public Properties

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
        private int _rowForSettingPanel;

        [ObservableProperty]
        private int _rowForMaintenancePanel;

        [ObservableProperty]
        private int _rowForLogisticPanel;

        [ObservableProperty]
        private int _rowForReasonDowntimePanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonElectricPanel;

        [ObservableProperty]
        private int _rowForDowntimeReasonSettingPanel;

        [ObservableProperty]
        private int _rowForCallForServicePanel;

        [ObservableProperty]
        private int _rowForServiceArrivalPanel;

        [ObservableProperty]
        private bool _downtimePanelIsOpen = false;

        [ObservableProperty]
        private bool _settingPanelIsOpen = false;

        [ObservableProperty]
        private bool _optionsPanelIsOpen = false;

        [ObservableProperty]
        private bool _maintenancePanelIsOpen = false;

        [ObservableProperty]
        private bool _logisticPanelIsOpen = false;

        [ObservableProperty]
        private bool _reasonDowntimePanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonElectricPanelIsOpen = false;

        [ObservableProperty]
        private bool _downtimeReasonSettingPanelIsOpen = false;

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
        private bool _controlPanelVisible = false;

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

        [ObservableProperty] private bool isTimeStampFromiPC = true;

        [ObservableProperty] private bool isLoginToApp = false;

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
        private ObservableGroupedCollection<string, ReasonDowntimePanelItem> _reasonDowntimePanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonElectricPanelItem> _downtimeReasonElectricPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, DowntimeReasonSettingPanelItem> _downtimeReasonSettingPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, SplunkPanelItem> _splunkPanel = default!;

        [ObservableProperty]
        private ObservableGroupedCollection<string, CallForServicePanelItem> _callForServicePanel = default!;

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
        [NotifyPropertyChangedFor(nameof(DowntimeReasonElectricPanelButtonText))]
        private DowntimeReasonElectricPanelItem? _selectedDowntimeReasonElectricPanelItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DowntimeReasonSettingPanelButtonText))]
        private DowntimeReasonSettingPanelItem? _selectedDowntimeReasonSettingPanelItem;

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

        public string ReasonDowntimePanelButtonText => SelectedReasonDowntimePanelItem?.Name ?? "Reason Downtime";

        public string DowntimeReasonElectricPanelButtonText => SelectedDowntimeReasonElectricPanelItem?.Name ?? "Downtime Reason Electric";

        public string DowntimeReasonSettingPanelButtonText => SelectedDowntimeReasonSettingPanelItem?.Name ?? "Downtime Reason Setting";

        public string CallForServicePanelButtonText => SelectedCallForServicePanelItem?.Name ?? "Call For Service";
        public string ServiceArrivalPanelButtonText => SelectedServiceArrivalPanelItem?.Name ?? "Service Arrival";

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
        public void DowntimeReasonElectricPanelButtonPressed() => DowntimeReasonElectricPanelIsOpen ^= true;

        [RelayCommand]
        public void DowntimeReasonSettingPanelButtonPressed() => DowntimeReasonSettingPanelIsOpen ^= true;

        [RelayCommand]
        public void CallForServicePanelButtonPressed() => CallForServicePanelIsOpen ^= true;

        [RelayCommand]
        public void SplunkPanelButtonPressed() => SplunkPanelIsOpen ^= true;



        [RelayCommand]
        private void DowntimePanelItemPressed(DowntimePanelItem item)
        {
            const string colorDowntime = "#DC4E41";

            if (_lastMessage?.Name == "S1.MachineDowntime")
            {
                ActualValueForWarrningNoticePopup();
                DowntimePanelIsOpen = false;
                return;
            }
            // Update the selected item 
            SelectedDowntimePanelItem = item;

            CallForServiceButtonIsVisible = true;
            DowntimeIsActive = true;

            // Close the menu 
            DowntimePanelIsOpen = false;
            CallForServicePanelIsOpen = true;

            ChangePropertyButtonStatus(colorDowntime, item.Status);
            PassMessageToRepository(item);
            ControlPanelVisible = true;
        }

        [RelayCommand]
        private async void SettingPanelItemPressed(SettingPanelItem item)
        {
            const string colorSetting = "#EE82EE";

            if (_lastMessage?.Name == "S1.MachineDowntime")
            {
                ActualValueForWarrningNoticePopup();
                SettingPanelIsOpen = false;
                return;
            }


            // Update the selected item 
            SelectedSettingPanelItem = item;

            // Close the menu 
            SettingPanelIsOpen = false;

            ChangePropertyButtonStatus(colorSetting, item.Status);
            PassMessageToRepository(item);
        }

        [RelayCommand]
        private async void MaintenancePanelItemPressed(MaintenancePanelItem item)
        {
            const string colorMaintenance = "#81EEEE";

            if (_lastMessage?.Name == "S1.MachineDowntime")
            {
                ActualValueForWarrningNoticePopup();
                MaintenancePanelIsOpen = false;
                return;
            }

            // Update the selected item 
            SelectedMaintenancePanelItem = item;

            // Close the menu 
            MaintenancePanelIsOpen = false;

            ChangePropertyButtonStatus(colorMaintenance, item.Status);
            PassMessageToRepository(item);
        }

        [RelayCommand]
        private void LogisticPanelItemPressed(LogisticPanelItem item)
        {
            const string colorLogistic = "#E1D747";

            if (_lastMessage?.Name == "S1.MachineDowntime")
            {
                ActualValueForWarrningNoticePopup();
                LogisticPanelIsOpen = false;
                return;
            }
            // Update the selected item 
            SelectedLogisticPanelItem = item;

            // Close the menu 
            LogisticPanelIsOpen = false;

            ChangePropertyButtonStatus(colorLogistic, item.Status);
            PassMessageToRepository(item);
        }

        [RelayCommand]
        private async void ActualStatusButtonPressed()
        {
            if (_lastMessage == null || DataIsSendingToSplunk) 
            {
                ShowMessageBox("Oczekiwanie na odpowiedź ze Splunka");
                await Task.Delay(3000);
                return;
            }
            if (DowntimeIsActive && _lastMessage.Status == "MECHANICZNA")
            {
                ReasonDowntimePanelIsOpen = true;
                ControlPanelVisible = true;
            }
            else if (DowntimeIsActive && _lastMessage.Status == "ELEKTRYCZNA")
            {
                DowntimeReasonElectricPanelIsOpen = true;
                ControlPanelVisible = true;
            }
            else if (DowntimeIsActive && _lastMessage.Status == "USTAWIACZ")
            {
                DowntimeReasonSettingPanelIsOpen = true;
                ControlPanelVisible = true;
            }
            else
            {
                _lastMessage.Value = "false";
                ClearActualButtonStatus();
                PassMessageToRepository(_lastMessage);
            }
        }

        private void ShowMessageBox(string message)
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
        private void ReasonDowntimePanelItemPressed(ReasonDowntimePanelItem item)
        {
            // Update the selected item 
            SelectedReasonDowntimePanelItem = item;

            // Close the menu 
            ReasonDowntimePanelIsOpen = false;
            ControlPanelVisible = false;
            ServiceCalled = false;
            ServiceArrival = false;
            PassMessageToRepository(item);
            ClearActualButtonStatus();
        }

        [RelayCommand]
        private void DowntimeReasonElectricPanelItemPressed(DowntimeReasonElectricPanelItem item)
        {
            // Update the selected item 
            SelectedDowntimeReasonElectricPanelItem = item;

            // Close the menu 
            DowntimeReasonElectricPanelIsOpen = false;
            ControlPanelVisible = false;
            ServiceCalled = false;
            ServiceArrival = false;
            PassMessageToRepository(item);
            ClearActualButtonStatus();
        }

        [RelayCommand]
        private void DowntimeReasonSettingPanelItemPressed(DowntimeReasonSettingPanelItem item)
        {
            // Update the selected item 
            SelectedDowntimeReasonSettingPanelItem = item;

            // Close the menu 
            DowntimeReasonSettingPanelIsOpen = false;
            ControlPanelVisible = false;
            ServiceCalled = false;
            ServiceArrival = false;
            PassMessageToRepository(item);
            ClearActualButtonStatus();
        }


        [RelayCommand]
        private void CallForServicePanelItemPressed(CallForServicePanelItem item)
        {
            // Update the selected item 
            SelectedCallForServicePanelItem = item;

            if (item != null && item.Value == "true")
            {
                CallServiceStatus();
                ServiceCalled = true;
            }

            CallForServicePanelIsOpen = false;
            ControlPanelVisible = false;
            PassMessageToRepository(item);
        }

        [RelayCommand]
        private void CallForServiceFromNavigationMenu()
        {
            if (ServiceCalled) return;
            CallForServicePanelIsOpen = true;
            ControlPanelVisible = true;
        }

        [RelayCommand]
        private void ServiceArrivalPanelItemPressed(ServiceArrivalPanelItem item)
        {
            SelectedServiceArrivalPanelItem = item;

            ServiceArrivalPanelIsOpen = false;

            ControlPanelVisible = false;
            if (item != null && item.Value == "true")
            {
                ServiceArrivalStatus();
                ServiceArrival = true;
            }

            PassMessageToRepository(item);
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

        [RelayCommand]
        private void ExitPressed()
        {
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

        private void TimerScheduleLoggin_Tick(object? sender, EventArgs e)
        {
            int[] targetTimesInMinutes = { 5 * 60 + 30, 13 * 60 + 30, 21 * 60 + 30 }; // 5:30, 13:30, 21:30

            TimeSpan currentTimeOfDay = DateTime.Now.TimeOfDay;


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
                    message.Value = "true";
                    message.OperatorName = LoginLabel ?? null;
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
                LoginLabel = EnteredLogin;
                EnteredLogin = "";
                LoginPanelIsOpen = false;
                ControlPanelVisible = false;

                MessageInformationToSplunk message = new MessageInformationToSplunk();
                message.Name = "S11.LoginToApp";
                message.Status = "Login";
                message.Value = "true";
                message.OperatorName = LoginLabel;
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
            message.Value = "true";
            message.OperatorName = LoginLabel ?? null;
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
            var statusPanel = await mStatusInterfaceService.GetDowntimePanelAsync();
            var settingStatusPanel = await mStatusInterfaceService.GetSettingPanelAsync();
            var maintenanceStatusPanel = await mStatusInterfaceService.GetMaintenancePanelAsync();
            var logisticStatusPanel = await mStatusInterfaceService.GetLogisticPanelAsync();
            var reasonDowntimeStatusPanel = await mStatusInterfaceService.GetReasonDowntimePanelAsync();
            var splunkStatusPanel = await mStatusInterfaceService.GetSplunkPanelAsync();
            var callForServicePanel = await mStatusInterfaceService.GetCallForServicePanelAsync();
            var serviceArrivalPanel = await mStatusInterfaceService.GetServiceArrivalPanelAsync();
            var downtimeReasonElectricPanel = await mStatusInterfaceService.GetDowntimeReasonElectricPanelAsync();
            var downtimeReasonSettingPanel = await mStatusInterfaceService.GetDowntimeReasonSettingPanelAsync();


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

            ReasonDowntimePanel =
                new ObservableGroupedCollection<string, ReasonDowntimePanelItem>(
                    reasonDowntimeStatusPanel.GroupBy(item => item.Name));

            RowForReasonDowntimePanel = LoadSizeOfGrid(reasonDowntimeStatusPanel.Count);

            DowntimeReasonElectricPanel =
                new ObservableGroupedCollection<string, DowntimeReasonElectricPanelItem>(
                    downtimeReasonElectricPanel.GroupBy(item => item.Name));

            RowForDowntimeReasonElectricPanel = LoadSizeOfGrid(downtimeReasonElectricPanel.Count);

            DowntimeReasonSettingPanel =
                new ObservableGroupedCollection<string, DowntimeReasonSettingPanelItem>(
                    downtimeReasonSettingPanel.GroupBy(item => item.Name));

            RowForDowntimeReasonSettingPanel = LoadSizeOfGrid(downtimeReasonSettingPanel.Count);

            SplunkPanel =
                new ObservableGroupedCollection<string, SplunkPanelItem>(
                    splunkStatusPanel.GroupBy(item => item.Group));

            CallForServicePanel =
                new ObservableGroupedCollection<string, CallForServicePanelItem>(
                    callForServicePanel.GroupBy(item => item.Name));

            RowForCallForServicePanel = LoadSizeOfGrid(callForServicePanel.Count);

            ServiceArrivalPanel =
                new ObservableGroupedCollection<string, ServiceArrivalPanelItem>(
                    serviceArrivalPanel.GroupBy(item => item.Name));

            RowForServiceArrivalPanel = LoadSizeOfGrid(serviceArrivalPanel.Count);
        }

        private int LoadSizeOfGrid(int numberOfElements)
        {
            int maxColumns = 3;

            int rows = (int)Math.Ceiling((double)numberOfElements / maxColumns);

            return rows;
        }

        public void ActualValueForWarrningNoticePopup()
        {
            WarrningPanelIsOpen = true;
            WarrningNoticeText = _lastMessage.Status;
            WarrningNoticeColor = ActualStatusColor;
        }

        public void ChangePropertyButtonStatus(string colorButton, string textButton)
        {
            ActualStatusButtonIsVisible = true;
            ActualStatusButtonText = textButton;
            ActualStatusColor = colorButton;
            ControlPanelVisible = false;
        }

        private void ClearActualButtonStatus()
        {
            ActualStatusButtonIsVisible = false;
            ActualStatusButtonText = "";
            ServiceArrivalButtonIsVisible = false;
            CallForServiceButtonIsVisible = false;
            DowntimeIsActive = false;
            ClearButtonsService();
        }
        private void ClearButtonsService()
        {
            CallForServiceButtonText = "WEZWIJ UR";
            CallForServiceButtonIsVisible = false;
            CallForServiceColor = "#DC4E41";
            ServiceArrivalButtonColor = "#DC4E41";
            ServiceArrivalButtonIsVisible = false;
            ServiceArrivalButtonText = "OCZEKIWANIE NA UR";
        }

        private void CallServiceStatus()
        {
            ServiceArrivalButtonIsVisible = true;
            CallForServiceButtonText = "UR WEZWANE";
            CallForServiceColor = "#008000";
        }

        private void ServiceArrivalStatus()
        {
            ServiceArrivalButtonText = "UR NA MIEJSCU";
            ServiceArrivalButtonColor = "#008000";
        }



        /// <summary>
        /// After the refactoring
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        //public async void PassMessageToRepository<T>(T message)
        //    where T : class, IEntity
        //{
        //    if (!repositories.ContainsKey(typeof(T)))
        //    {
        //        repositories[typeof(T)] = new GenericRepository<T>();
        //    }

        //    var repository = repositories[typeof(T)] as GenericRepository<T>;

        //    switch (sentMessageWithTrue)
        //    {
        //        case true when message != null && message.Value == "false" && message.Name != "S1.MachineDowntime" && message.Name != "S7.CallForService" && message.Name != "S7.ServiceArrival":
        //            await SendMessageToSplunk(message);
        //            //repository.Add(message);
        //            sentMessageWithTrue = false;
        //            message.Value = "true";
        //            _lastMessage = null;
        //            return;
        //        case true when _lastMessage != null && message.Name != "S7.CallForService" && message.Name != "S7.ServiceArrival":
        //            _lastMessage.Value = "false";
        //            await SendMessageToSplunk(_lastMessage);
        //            _lastMessage.Value = "true";
        //            //repository.Add(_lastMessage);
        //            sentMessageWithTrue = false;
        //            break;
        //    }

        //    if (_lastMessage?.Name == "S1.MachineDowntime" && message.Name is "S7.CallForService" or "S7.ServiceArrival")
        //    {
        //        message.Status = _lastMessage.Status;
        //        await SendMessageToSplunk(message);
        //        repository.Add(message);
        //        if (message.Name is "S7.CallForService" or "S7.ServiceArrival")
        //        {
        //            return;
        //        }
        //        sentMessageWithTrue = false;
        //        message.Value = "true";
        //    }
        //    else
        //    {
        //        message.Value = "true";
        //        await SendMessageToSplunk(message);
        //        repository.Add(message);
        //        sentMessageWithTrue = true;
        //        if (message.Name == "S7.ReasonDowntime")
        //        {
        //            _lastMessage = null;
        //            return;
        //        }
        //        _lastMessage = message;
        //    }
        //}


        /// <summary>
        /// Before the refactoring
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        public async void PassMessageToRepository<T>(T message)
          where T : class, IEntity
        {
            if (!repositories.ContainsKey(typeof(T)))
            {
                repositories[typeof(T)] = new GenericRepository<T>();
            }

            var repository = repositories[typeof(T)] as GenericRepository<T>;

            if (sentMessageWithTrue && message != null && message.Value == "false" && message.Name != "S1.MachineDowntime" && message.Name != "S7.CallForService" && message.Name != "S7.ServiceArrival")
            {
                await SendMessageToSplunk(message);
                //repository.Add(message);
                sentMessageWithTrue = false;
                message.Value = "true";
                _lastMessage = null;
                return;
            }

            if (sentMessageWithTrue && _lastMessage != null && message.Name != "S7.CallForService" && message.Name != "S7.ServiceArrival")
            {
                _lastMessage.Value = "false";
                await SendMessageToSplunk(_lastMessage);
                _lastMessage.Value = "true";
                //repository.Add(_lastMessage);
                sentMessageWithTrue = false;
            }
            if (_lastMessage != null && _lastMessage.Name == "S1.MachineDowntime" && (message.Name == "S7.CallForService" || message.Name == "S7.ServiceArrival"))
            {
                message.Status = _lastMessage.Status;
                await SendMessageToSplunk(message);
                repository.Add(message);
                if (message.Name == "S7.CallForService" || message.Name == "S7.ServiceArrival")
                {
                    return;
                }
                sentMessageWithTrue = false;
                message.Value = "true";
            }
            else
            {
                message.Value = "true";
                await SendMessageToSplunk(message);
                repository.Add(message);
                sentMessageWithTrue = true;
                if (message.Name == "S7.DowntimeReason")
                {
                    _lastMessage = null;
                    return;
                }
                _lastMessage = message;
            }
        }

        public async Task SendMessageToSplunk<T>(T message)
        {
            var splunkLogger = new GenericSplunkLogger<T>(this);
            await splunkLogger.LogAsync(message);
        }

        public void LoadPageSap()
        {
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

        public void LoginPanelOpen()
        {
            LoginPanelIsOpen = true;
        }

        private void KeepAlive(object state)
        {
            MessageInformationToSplunk message = new MessageInformationToSplunk();
            message.Name = "S11.KeepAlive";
            message.Status = "live";
            message.Value = "true";
            message.OperatorName = LoginLabel?? null;
            SendMessageToSplunk(message);
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
            _sharedDataService = new SharedDataService();
            appConfig = _sharedDataService.AppConfig;
            BarOnTopApp = $"{appConfig.Line}  /  {appConfig.WorkplaceName}";
            sapUrl = appConfig.UrlSap;
            LoadPageSap();
            if (double.TryParse(appConfig.SpanForKeepAlive, out double keepAliveMinutes))
            {
                _timer = new Timer(KeepAlive, null, TimeSpan.Zero, TimeSpan.FromMinutes(keepAliveMinutes));
            }
            else
            {
                _timer = new Timer(KeepAlive, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            }
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
