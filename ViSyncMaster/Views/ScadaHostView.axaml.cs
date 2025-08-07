using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace ViSyncMaster;

public partial class ScadaHostView : UserControl
{
    private Process? _scadaProcess;
    private IntPtr _scadaHandle = IntPtr.Zero;

    public ScadaHostView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e) => StartScada();

    private void StartScada()
    {
        var visionLauncherPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Inductive Automation",
            "Vision Client Launcher",
            "visionclientlauncher.exe");

        var scadaDirectory = Path.Combine(
            Path.GetPathRoot(Environment.SystemDirectory) ?? "C:",
            "ViSM",
            "SCADA");

        var psi = new ProcessStartInfo
        {
            FileName = visionLauncherPath,
            Arguments = $"-Dapp.home=\"{scadaDirectory}\" -Dapplication=W16_SCADA",
            UseShellExecute = true
        };

        _scadaProcess = Process.Start(psi);
        if (_scadaProcess == null) return;

        _scadaProcess.WaitForInputIdle();
        _scadaHandle = _scadaProcess.MainWindowHandle;

        if (OperatingSystem.IsWindows())
        {
            var topLevel = TopLevel.GetTopLevel(ScadaContainer);
            var hostHandle = topLevel?.PlatformImpl?.Handle.Handle ?? IntPtr.Zero;

            SetParent(_scadaHandle, hostHandle);
            SetWindowLong(_scadaHandle, GWL_STYLE, WS_VISIBLE);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    private const int GWL_STYLE = -16;
    private const uint WS_VISIBLE = 0x10000000;
}