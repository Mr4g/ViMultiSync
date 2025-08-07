using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ViSyncMaster.Views.Controls
{
    public class ScadaHostNative : NativeControlHost
    {
        private Process? _proc;
        private IntPtr _childHwnd;

        // --- WinAPI ---
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(
            IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);

        private const int GWL_STYLE = -16;
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;

        public async Task StartAsync()
        {
            var launcher = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Inductive Automation",
                "Vision Client Launcher",
                "visionclientlauncher.exe");

            if (!File.Exists(launcher))
                throw new FileNotFoundException("Nie znaleziono Vision Client Launcher", launcher);

            var psi = new ProcessStartInfo
            {
                FileName = launcher,
                Arguments = "application=W16_SCADA window.mode=window",
                WorkingDirectory = Path.GetDirectoryName(launcher),
                UseShellExecute = false,
                RedirectStandardError = true
            };

            _proc = Process.Start(psi);
            if (_proc == null) return;

            _proc.WaitForInputIdle();
            while (_proc.MainWindowHandle == IntPtr.Zero)
                await Task.Delay(50);

            _childHwnd = _proc.MainWindowHandle;
            // dalej Avalonia wywoła CreateNativeControlCore
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var parentHwnd = parent.Handle;
            // nadaj WS_CHILD|WS_VISIBLE, usuń ramki
            var style = GetWindowLong(_childHwnd, GWL_STYLE);
            style = (style | WS_CHILD | WS_VISIBLE) & ~(WS_BORDER | WS_CAPTION);
            SetWindowLong(_childHwnd, GWL_STYLE, style);

            SetParent(_childHwnd, parentHwnd);

            // wyrenderuj i skaluj
            var r = Bounds;
            MoveWindow(_childHwnd, 0, 0, (int)r.Width, (int)r.Height, true);

            return new PlatformHandle(_childHwnd, "HWND");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (_proc is { HasExited: false })
                _proc.Kill();
            _proc?.Dispose();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _ = StartAsync();
            LayoutUpdated += (_, __) =>
            {
                if (_childHwnd != IntPtr.Zero)
                {
                    var b = Bounds;
                    MoveWindow(_childHwnd, 0, 0, (int)b.Width, (int)b.Height, true);
                }
            };
        }
    }
}
