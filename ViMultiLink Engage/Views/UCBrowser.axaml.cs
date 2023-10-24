using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Xilium.CefGlue.Avalonia;
using System.Reflection;
using Avalonia.Input;
using Avalonia.Media;
using Xilium.CefGlue.Common;
using Xilium.CefGlue;

namespace ViMultiSync.Views;

public partial class UCBrowser : UserControl
{


    private AvaloniaCefBrowser browser;
    public event Action<string> TitleChanged;
    public UCBrowser()
    {
        InitializeComponent();


        StartBrowser();
        ZIndex=1;
    }



    public void StartBrowser()
    {
        CefRuntimeLoader.Initialize(new CefSettings
        {
            WindowlessRenderingEnabled = true
            // Dodaj inne ustawienia CEF wed³ug potrzeb
        });

        var browserWrapper = this.FindControl<Decorator>("browserWrapper");

        browser = new AvaloniaCefBrowser();

        browser.Address = "http://ps093w05.viessmann.net:51300/pod-me/com/sap/me/wpmf/client/template.jsf?WORKSTATION=WORK_CENTER_TOUCH_TEST&SOFT_KEYBOARD=true&ACTIVITY_ID=ZVI_WC_POD_COPPER&sap-lsf-PreferredRendering=standards#";
        browser.RegisterJavascriptObject(new BindingTestClass(), "boundBeforeLoadObject");
        browser.LoadStart += OnBrowserLoadStart;
        browser.TitleChanged += OnBrowserTitleChanged;
        browserWrapper.Child = browser;
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

            addressTextBox.Text = e.Frame.Url;
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
}