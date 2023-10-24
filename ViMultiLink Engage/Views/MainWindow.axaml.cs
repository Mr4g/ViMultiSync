using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ViMultiSync.ViewModels;

namespace ViMultiSync.Views
{
    public partial class MainWindow : Window
    {

        #region Private Members

        private Control mMainGrid;
        private Control mDowntimePanelPopup;
        private Control mDowntimePanelButton;

        #endregion
        public MainWindow()
        {
            InitializeComponent();

            mDowntimePanelButton = this.FindControl<Control>("DowntimePanelButton") ?? throw new Exception("Cannot find Channel Configuration Button by name ");
            mDowntimePanelPopup = this.FindControl<Control>("DowntimePanelPopup") ?? throw new Exception("Cannot find Channel Configuration Popup by name ");
            mMainGrid = this.FindControl<Control>("MainGrid") ?? throw new Exception("Cannot find Channel Configuration MainGrid by name ");
            this.Opened += OnOpened;
            this.Resized += MainWindow_Resized;

        }


        private async void OnOpened(object sender, EventArgs e)
        {
            await ((MainWindowViewModel)DataContext).LoadSettingsCommand.ExecuteAsync(null);
        }

        private void MainWindow_Resized(object sender, EventArgs e)
        {
            var position = mDowntimePanelButton.TranslatePoint(new Point(), MainGrid) ?? throw new Exception("Cannot get TranslatePoint from Configuration");

            mDowntimePanelPopup.Margin = new Thickness(
                position.X + mDowntimePanelButton.Bounds.Right,
                position.Y,
                0,
               0);
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e) =>
            ((MainWindowViewModel)DataContext).DowntimePanelButtonPressedCommand.Execute(null);


    }
}