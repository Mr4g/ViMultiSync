using System;
using System.ComponentModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ShimSkiaSharp;
using Tmds.DBus.Protocol;
using WebViewControl;


namespace ViMultiSync.Views;

public partial class UCWebBrowser : UserControl
{
    public UCWebBrowser()
    {
        InitializeComponent();
        var browserWrapper = this.FindControl<WebView>("webwiew");
        StarWebPage(browserWrapper);
    }

    public void StarWebPage(WebView webView )
    {
       
        webView.Address = "http://www.example.com";
    }


}