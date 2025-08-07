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
            "ViSM", "SCADA");

        var psi = new ProcessStartInfo
        {
            FileName = visionLauncherPath,
            Arguments = "application=W16_SCADA window.mode=window",
            WorkingDirectory = Path.GetDirectoryName(visionLauncherPath),
            UseShellExecute = false,                // ← false, żeby Redirect… zadziałało
            RedirectStandardError = true,
        };

        _scadaProcess = Process.Start(psi);
        if (_scadaProcess == null) return;

        _scadaProcess.WaitForInputIdle();
        _scadaHandle = _scadaProcess.MainWindowHandle;

        if (OperatingSystem.IsWindows())
        {
            var topLevel = TopLevel.GetTopLevel(ScadaContainer);
            var hostHwnd = topLevel!.TryGetPlatformHandle()!.Handle;

            // 1) ustawiamy WS_CHILD, usuwamy WS_BORDER i WS_CAPTION
            var style = GetWindowLong(_scadaHandle, GWL_STYLE);
            style = (style | WS_CHILD | WS_VISIBLE)
                    & ~(WS_BORDER | WS_CAPTION);
            SetWindowLong(_scadaHandle, GWL_STYLE, style);

            // 2) podpinamy pod hosta Avalonia
            SetParent(_scadaHandle, hostHwnd);

            // 3) ustawiamy rozmiar i pozycję (możesz to też wrzucić w LayoutUpdated)
            var rect = ScadaContainer.Bounds;
            MoveWindow(_scadaHandle, 0, 0,
                       (int)rect.Width, (int)rect.Height, true);

            // 4) opcjonalnie: nasłuchuj zmiany rozmiaru:
            ScadaContainer.LayoutUpdated += (_, __) =>
            {
                var b = ScadaContainer.Bounds;
                MoveWindow(_scadaHandle, 0, 0,
                           (int)b.Width, (int)b.Height, true);
            };
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool MoveWindow(
        IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    const int GWL_STYLE = -16;
    const uint WS_VISIBLE = 0x10000000;
    const uint WS_CHILD = 0x40000000;
    const uint WS_BORDER = 0x00800000;
    const uint WS_CAPTION = 0x00C00000;
}