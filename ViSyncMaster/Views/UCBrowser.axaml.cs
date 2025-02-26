using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Xilium.CefGlue.Avalonia;
using System.Reflection;
using Avalonia.Input;
using Avalonia.Media;
using ViSyncMaster.AuxiliaryClasses;
using Xilium.CefGlue.Common;
using Xilium.CefGlue;
using Xilium.CefGlue.Common.Handlers;
using ViSyncMaster.Keyboard;
using Xilium.CefGlue.Common.Events;
using System.IO;

namespace ViSyncMaster.Views
{
    public partial class UCBrowser : UserControl
    {
        private bool _isTextBoxFocused = false;
        private VirtualKeyboardTextInputMethod virtualKeyboardTextInput = null;
        private AvaloniaCefBrowser browser;
        private double _zoomlevel;
        public event Action<string> TitleChanged;

        public UCBrowser(string link)
        {
            InitializeComponent();
            InitializeCef();
            virtualKeyboardTextInput = new VirtualKeyboardTextInputMethod();
            StartBrowser(link);
            ZIndex = 1;
        }

        public void ChangeBrowserAddress(string newUrl)
        {
            browser.Address = newUrl;
        }

        public void StartBrowser(string link)
        {
            var browserWrapper = this.FindControl<Decorator>("browserWrapper");

            browser = new AvaloniaCefBrowser();
            browser.Address = $"{link}";

            browser.RegisterJavascriptObject(new BindingTestClass(), "boundBeforeLoadObject");
            browser.LoadStart += OnBrowserLoadStart;
            browser.TitleChanged += OnBrowserTitleChanged;
            browserWrapper.Child = browser;
        }

        #region Private methods

        private void InitializeCef()
        {
            var settings = new CefSettings
            {
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyAppCache"),
                PersistSessionCookies = true,
                PersistUserPreferences = true
            };

            CefRuntimeLoader.Initialize(settings);
        }

        private void OnResetCookiesButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ResetCookies();
        }

        private void ResetCookies()
        {
            var cookieManager = CefCookieManager.GetGlobal(null);
            cookieManager.DeleteCookies("", "", new DeleteCookiesCallback());
        }

        private class DeleteCookiesCallback : CefDeleteCookiesCallback
        {
            protected override void OnComplete(int numDeleted)
            {
                Console.WriteLine($"{numDeleted} cookies deleted.");
            }
        }

        private class CookieCallback : CefSetCookieCallback
        {
            protected override void OnComplete(bool success)
            {
                if (success)
                {
                    Console.WriteLine("Cookie set successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to set cookie.");
                }
            }
        }

        private void OnBackButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (browser.CanGoBack)
            {
                browser.GoBack();
            }
        }

        private void OnForwardButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (browser.CanGoForward)
            {
                browser.GoForward();
            }
        }

        private void OnRefreshButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            browser.Reload();
        }

        private void OnZoomInButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _zoomlevel = _zoomlevel + 0.1;
            browser.ZoomLevel = _zoomlevel;
        }

        private void OnZoomOutButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _zoomlevel = _zoomlevel - 0.1;
            browser.ZoomLevel = _zoomlevel;
        }

        private void ResetZoomClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _zoomlevel = 0;
            browser.ZoomLevel = _zoomlevel;
        }

        private void OnDevToolsButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (browser != null)
            {
                browser.ShowDeveloperTools();
            }
        }

        private void OnBrowserLoadStart(object sender, Xilium.CefGlue.Common.Events.LoadStartEventArgs e)
        {
            if (e.Frame.Browser.IsPopup || !e.Frame.IsMain)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                var addressTextBox = this.FindControl<TextBox>("addressTextBox");
                var backButton = this.FindControl<Button>("backButton");
                var forwardButton = this.FindControl<Button>("forwardButton");

                addressTextBox.Text = e.Frame.Url;

                if (backButton != null)
                {
                    backButton.IsEnabled = browser.CanGoBack;
                }

                if (forwardButton != null)
                {
                    forwardButton.IsEnabled = browser.CanGoForward;
                }
            });
        }

        private void OnBrowserTitleChanged(object sender, string title)
        {
            TitleChanged?.Invoke(title);
        }

        private void OnAddressTextBoxKeyDown(object sender, global::Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                browser.Address = ((TextBox)sender).Text;
            }
        }

        #endregion
    }
}