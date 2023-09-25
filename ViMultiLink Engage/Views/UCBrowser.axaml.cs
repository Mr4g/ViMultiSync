using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Xilium.CefGlue.Avalonia;
using System.Reflection;
using Avalonia.Input;

namespace ViMultiSync.Views;

public partial class UCBrowser : UserControl
{

    private AvaloniaCefBrowser browser;
    public event Action<string> TitleChanged;
    public UCBrowser()
    {
        InitializeComponent();
        StartBrowser();
    }

    public void StartBrowser()
    {
 
        var browserWrapper = this.FindControl<Decorator>("browserWrapper");

        browser = new AvaloniaCefBrowser();
        browser.Address = "http://ps094w05.viessmann.net:51100/manufacturing/com/sap/me/activity/client/ActivityManager.jsp";
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