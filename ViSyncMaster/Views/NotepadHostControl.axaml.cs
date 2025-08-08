using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public partial class NotepadHostControl : UserControl
{
    [DllImport("user32.dll")]
    static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    [DllImport("user32.dll")]
    static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll", SetLastError = true)]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    const int GWL_STYLE = -16;
    const int WS_CHILD = 0x40000000;
    const int WS_VISIBLE = 0x10000000;

    Process _process;

    public NotepadHostControl()
    {
        InitializeComponent();

        Loaded += NotepadHostControl_Loaded;
        Unloaded += NotepadHostControl_Unloaded;
        SizeChanged += NotepadHostControl_SizeChanged;
    }

    private void NotepadHostControl_Loaded(object sender, RoutedEventArgs e)
    {
        _process = Process.Start("notepad.exe");
        _process.WaitForInputIdle();

        IntPtr hwnd = _process.MainWindowHandle;

        // Ustaw okno Notatnika jako dziecko hosta
        SetParent(hwnd, host.Handle);
        SetWindowLong(hwnd, GWL_STYLE, WS_VISIBLE | WS_CHILD);

        // Dopasuj rozmiar
        MoveWindow(hwnd, 0, 0, (int)ActualWidth, (int)ActualHeight, true);
    }

    private void NotepadHostControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_process?.MainWindowHandle != IntPtr.Zero)
            MoveWindow(_process.MainWindowHandle, 0, 0,
                       (int)ActualWidth, (int)ActualHeight, true);
    }

    private void NotepadHostControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_process != null && !_process.HasExited)
            _process.CloseMainWindow();
    }
}
