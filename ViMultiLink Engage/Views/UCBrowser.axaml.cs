using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Xilium.CefGlue.Avalonia;
using System.Reflection;
using Avalonia.Input;
using Avalonia.Media;
using ViMultiSync.AuxiliaryClasses;
using Xilium.CefGlue.Common;
using Xilium.CefGlue;
using Xilium.CefGlue.Common.Handlers;
using ViMultiSync.Keyboard;
using Xilium.CefGlue;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Events;

namespace ViMultiSync.Views;

public partial class UCBrowser : UserControl
{
    private bool _isTextBoxFocused = false;
    private VirtualKeyboardTextInputMethod virtualKeyboardTextInput = null;
    private AvaloniaCefBrowser browser;
    public event Action<string> TitleChanged;
    public UCBrowser(string link)
    {
        InitializeComponent();
        virtualKeyboardTextInput = new VirtualKeyboardTextInputMethod();
        StartBrowser(link);
        ZIndex=1;
    }

    public void ChangeBrowserAddress(string newUrl)
    {
        browser.Address = newUrl;
    }

    public void StartBrowser(string link)
    {
        //CefRuntimeLoader.Initialize(new CefSettings
        //{
        //    WindowlessRenderingEnabled = true
        //    // Dodaj inne ustawienia CEF wed³ug potrzeb
        //});

        var browserWrapper = this.FindControl<Decorator>("browserWrapper");

        browser = new AvaloniaCefBrowser();
        browser.Address = $"{link}";


        //var myKeyboardHandler = new MyKeyboardHandler();
        //var keyboardHandlerAdapter = new MyKeyboardHandlerAdapter(myKeyboardHandler);
        //browser.KeyboardHandler = keyboardHandlerAdapter;

        browser.RegisterJavascriptObject(new BindingTestClass(), "boundBeforeLoadObject");
        browser.LoadStart += OnBrowserLoadStart;
        browser.TitleChanged += OnBrowserTitleChanged;
        //browser.LoadEnd += OnFrameLoadEnd;
        browserWrapper.Child = browser;
    }

    #region Private methods

    //private void OnFrameLoadEnd(object sender, LoadEndEventArgs e)
    //{
    //    // Wstrzykujemy JavaScript po zakoñczeniu ³adowania strony
    //    browser.ExecuteJavaScript(@"
    //            jQuery.ajax({
    //                url: 'https://jira.viessmann.com/plugins/servlet/issueCollectorBootstrap.js?collectorId=55135dc2&locale=en_US',
    //                type: 'get',
    //                cache: true,
    //                dataType: 'script'
    //            });
    //        ");
    //}

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


    private void OnBrowserLoadStart(object sender, Xilium.CefGlue.Common.Events.LoadStartEventArgs e)
    {
        if (e.Frame.Browser.IsPopup || !e.Frame.IsMain)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
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