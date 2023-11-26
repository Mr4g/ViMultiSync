using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ViMultiSync.ViewModels;
using ViMultiSync.Keyboard;


namespace ViMultiSync.Views
{
    public partial class MainWindow : Window
    {
        private VirtualKeyboardTextInputMethod virtualKeyboardTextInput = null;

        #region Private Members

        private Control mMainGrid;
        private Control mDowntimePanelPopup;
        private Control mDowntimePanelButton;
        private Control mSettingPanelPopup;
        private Control mSettingPanelButton;
        private Control mMaintenacePanelPopup;
        private Control mMaintenancePanelButton;
        private Control mLogisticPanelPopup;
        private Control mLogisticPanelButton;
        private Control mSplunkPanelPopup;
        private Control mSplunkPanelButton;
        private Control mOptionsPanelButton;
        private Control mOptionsPanelPopup;


        #endregion

        public MainWindow()
        {
            InitializeComponent();

            this.SystemDecorations = SystemDecorations.None;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            virtualKeyboardTextInput = new VirtualKeyboardTextInputMethod((Window)this);
            this.AddHandler<GotFocusEventArgs>(Control.GotFocusEvent, openVirtualKeyboard);


            mDowntimePanelButton = this.FindControl<Control>("DowntimePanelButton") ??
                                   throw new Exception("Cannot find Channel Configuration Button by name ");
            mDowntimePanelPopup = this.FindControl<Control>("DowntimePanelPopup") ??
                                  throw new Exception("Cannot find Channel Configuration Popup by name ");

            mSettingPanelButton = this.FindControl<Control>("SettingPanelButton") ??
                                  throw new Exception("Cannot find Channel Configuration Button by name ");
            mSettingPanelPopup = this.FindControl<Control>("SettingPanelPopup") ??
                                 throw new Exception("Cannot find Channel Configuration Popup by name ");

            mMaintenancePanelButton = this.FindControl<Control>("MaintenancePanelButton") ??
                                      throw new Exception("Cannot find Channel Configuration Button by name ");
            mMaintenacePanelPopup = this.FindControl<Control>("MaintenancePanelPopup") ??
                                    throw new Exception("Cannot find Channel Configuration Popup by name ");

            mLogisticPanelButton = this.FindControl<Control>("LogisticPanelButton") ??
                                   throw new Exception("Cannot find Channel Configuration Button by name ");
            mLogisticPanelPopup = this.FindControl<Control>("LogisticPanelPopup") ??
                                  throw new Exception("Cannot find Channel Configuration Popup by name ");

            mSplunkPanelButton = this.FindControl<Control>("SplunkPanelButton") ??
                                 throw new Exception("Cannot find Channel Configuration Button by name ");
            mSplunkPanelPopup = this.FindControl<Control>("SplunkPanelPopup") ??
                                throw new Exception("Cannot find Channel Configuration Popup by name ");

            mOptionsPanelButton = this.FindControl<Control>("OptionsPanelButton") ??
                                  throw new Exception("Cannot find Channel Configuration Button by name ");
            mOptionsPanelPopup = this.FindControl<Control>("OptionsPanelPopup") ??
                                 throw new Exception("Cannot find Channel Configuration Popup by name ");

            mMainGrid = this.FindControl<Control>("MainGrid") ??
                        throw new Exception("Cannot find Channel Configuration MainGrid by name ");
            this.Opened += OnOpened;
            this.Resized += MainWindow_Resized;
        }

        private async void OnOpened(object sender, EventArgs e)
        {
            await Task.Delay(2000);
            this.WindowState = WindowState.Maximized;
            await Task.Delay(1000);
            this.Topmost = true;
            this.CanResize = false;
            await ((MainWindowViewModel)DataContext).LoadSettingsCommand.ExecuteAsync(null);
        }

        private void MainWindow_Resized(object sender, EventArgs e)
        {
            var position = mDowntimePanelButton.TranslatePoint(new Point(), MainGrid) ??
                           throw new Exception("Cannot get TranslatePoint from Configuration");

            mDowntimePanelPopup.Margin = new Thickness(
                position.X + mDowntimePanelButton.Bounds.Right,
                position.Y,
                0,
                0);

            var positionSettingButton = mSettingPanelButton.TranslatePoint(new Point(), MainGrid) ??
                                        throw new ArgumentException("Cannot get TranslatePoint from Configuration");


            mSettingPanelPopup.Margin = new Thickness(
                positionSettingButton.X + mSettingPanelButton.Bounds.Right,
                positionSettingButton.Y,
                0,
                20);

            var positionMaintenanceButton = mMaintenancePanelButton.TranslatePoint(new Point(), MainGrid) ??
                                            throw new ArgumentException("Cannot get TranslatePoint from Configuration");


            mMaintenacePanelPopup.Margin = new Thickness(
                positionMaintenanceButton.X + mMaintenancePanelButton.Bounds.Right,
                positionMaintenanceButton.Y,
                0,
                20);

            var positionLogisticButton = mLogisticPanelButton.TranslatePoint(new Point(), MainGrid) ??
                                         throw new ArgumentException("Cannot get TranslatePoint from Configuration");


            mLogisticPanelPopup.Margin = new Thickness(
                positionLogisticButton.X + mLogisticPanelButton.Bounds.Right,
                positionLogisticButton.Y,
                0,
                20);

            var positionSplunkButton = mSplunkPanelButton.TranslatePoint(new Point(), MainGrid) ??
                                       throw new ArgumentException("Cannot get TranslatePoint from Configuration");


            mSplunkPanelPopup.Margin = new Thickness(
                0,
                positionSplunkButton.Y + mSplunkPanelButton.Bounds.Height,
                mSplunkPanelButton.Bounds.Width,
                20);

            var positionOptionsButton = mSplunkPanelButton.TranslatePoint(new Point(), MainGrid) ??
                                        throw new ArgumentException("Cannot get TranslatePoint from Configuration");

            mOptionsPanelPopup.Margin = new Thickness(
                0,
                positionOptionsButton.Y + mOptionsPanelButton.Bounds.Height,
                mOptionsPanelButton.Bounds.Width,
                20);
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) =>
            ((MainWindowViewModel)DataContext).DowntimePanelButtonPressedCommand.Execute(null);

        private void openVirtualKeyboard(object? sender, GotFocusEventArgs e)
        {
            //if (e.Source.GetType() == typeof(TextBox))
            //{
            //    FocusManager.ClearFocus();
            //    virtualKeyboardTextInput.SetActive(e);
            //}

            if (e.Source is TextBox textBox)
            {
                bool isPasswordChar = false;
                if (textBox.PasswordChar == '*')
                    isPasswordChar = true;


                // TextBox ma ustawiony PasswordChar na '*'
                FocusManager.ClearFocus();
                virtualKeyboardTextInput.SetActive(e, isPasswordChar);
                isPasswordChar = false;
            }
        }
    }
}
