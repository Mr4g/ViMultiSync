using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Views.Controls
{
    public class ScadaHostNative : NativeControlHost
    {
        private Process? _proc;
        private IntPtr _childHwnd;

        // --- WinAPI constants ---
        private const int GWL_STYLE = -16;
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint GW_OWNER = 4;

        // --- WinAPI delegates/functions ---
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(
            IntPtr hWnd,
            int X,
            int Y,
            int Width,
            int Height,
            bool Repaint);

        /// <summary>
        /// Finds the top-level window of the SCADA application by title.
        /// </summary>
        private IntPtr FindScadaWindow(int pid)
        {
            IntPtr result = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint windowPid);
                if (windowPid != pid)
                    return true;

                if (!IsWindowVisible(hWnd) || GetWindow(hWnd, GW_OWNER) != IntPtr.Zero)
                    return true;

                int len = GetWindowTextLength(hWnd);
                if (len <= 0)
                    return true;

                var sb = new StringBuilder(len + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                // Look for the final SCADA window title prefix
                if (title.StartsWith("W16 SCADA", StringComparison.OrdinalIgnoreCase) ||
                    title.IndexOf("SCADA", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result = hWnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);
            return result;
        }

        /// <summary>
        /// Starts the Vision SCADA process and waits until its main window appears.
        /// </summary>
        public async Task StartAsync()
        {
            var launcher = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Inductive Automation",
                "Vision Client Launcher",
                "visionclientlauncher.exe");

            if (!File.Exists(launcher))
                throw new FileNotFoundException("Vision Client Launcher not found.", launcher);

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

            // Wait up to 60s for the real SCADA window
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(60))
            {
                var h = FindScadaWindow(_proc.Id);
                if (h != IntPtr.Zero)
                {
                    _childHwnd = h;
                    break;
                }
                await Task.Delay(500);
            }

            if (_childHwnd == IntPtr.Zero)
                throw new TimeoutException("Timed out waiting for SCADA main window.");
            // Then Avalonia calls CreateNativeControlCore
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            //Debug.Assert(_childHwnd != IntPtr.Zero, "No SCADA window handle.");

            var parentHwnd = parent.Handle;

            var style = GetWindowLong(_childHwnd, GWL_STYLE);
            style = (style | WS_CHILD | WS_VISIBLE) & ~(WS_BORDER | WS_CAPTION);
            SetWindowLong(_childHwnd, GWL_STYLE, style);

            SetParent(_childHwnd, parentHwnd);

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
