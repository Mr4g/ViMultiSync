using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViSyncMaster.Services
{
    public sealed class ScadaProcessManager
    {
        public static ScadaProcessManager Instance { get; } = new();

        private ScadaProcessManager() { }

        // KONFIGURACJA – ustaw raz przy starcie (App.axaml.cs)
        public string StartPath { get; set; } = @"C:\ViSM\SCADA\W16 SCADA.lnk"; // .lnk/.exe/.bat
        public string? StartArguments { get; set; } = null;
        public string WindowTitleMatch { get; set; } = "W16 SCADA";

        // Stan
        private readonly SemaphoreSlim _gate = new(1, 1);
        private Process? _proc;
        private IntPtr _childHwnd;

        public IntPtr ScadaMainWindowHandle => _childHwnd;

        // WinAPI
        private const int GWL_STYLE = -16;
        private const uint WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000, WS_BORDER = 0x00800000, WS_CAPTION = 0x00C00000;
        private const uint GW_OWNER = 4;

        [DllImport("user32.dll", SetLastError = true)] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
        [DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)] private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] private static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int W, int H, bool repaint);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLengthW(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder sb, int maxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static string? GetTitle(IntPtr h)
        {
            int n = GetWindowTextLengthW(h);
            if (n <= 0) return null;
            var sb = new StringBuilder(n + 1);
            GetWindowTextW(h, sb, sb.Capacity);
            return sb.ToString();
        }

        // Uwaga: nie filtrujemy IsWindowVisible – łapiemy też zminimalizowane/ukryte
        private IntPtr FindWindowByTitleOnly()
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((h, _) =>
            {
                if (!IsWindow(h)) return true;
                if (GetWindow(h, GW_OWNER) != IntPtr.Zero) return true;
                var title = GetTitle(h) ?? string.Empty;
                if (title.IndexOf(WindowTitleMatch, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = h;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        public async Task<bool> EnsureStartedAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (_childHwnd != IntPtr.Zero && IsWindow(_childHwnd))
                    return true;

                _childHwnd = FindWindowByTitleOnly();
                if (_childHwnd != IntPtr.Zero && IsWindow(_childHwnd))
                    return true;

                if (_proc is null || _proc.HasExited)
                {
                    if (!File.Exists(StartPath)) return false;

                    var psi = new ProcessStartInfo
                    {
                        FileName = StartPath,
                        Arguments = StartArguments ?? "",
                        UseShellExecute = true,
                        Verb = "open",
                        WorkingDirectory = Path.GetDirectoryName(StartPath)!,
                    };
                    _proc = Process.Start(psi);
                    _proc?.WaitForInputIdle(5000);
                }

                var sw = Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(120))
                {
                    _childHwnd = FindWindowByTitleOnly();
                    if (_childHwnd != IntPtr.Zero && IsWindow(_childHwnd))
                        return true;

                    await Task.Delay(300);
                }
                return false;
            }
            finally { _gate.Release(); }
        }

        /// Przepina istniejące okno do danego hosta i dopasowuje rozmiar.
        public void AttachToHost(IntPtr hostHandle, int pixelW, int pixelH)
        {
            if (_childHwnd == IntPtr.Zero || !IsWindow(_childHwnd)) return;

            var style = (nuint)GetWindowLongPtr(_childHwnd, GWL_STYLE);
            style = (style | WS_CHILD | WS_VISIBLE) & ~(WS_BORDER | WS_CAPTION);
            SetWindowLongPtr(_childHwnd, GWL_STYLE, (nint)style);

            SetParent(_childHwnd, hostHandle);
            MoveWindow(_childHwnd, 0, 0, pixelW, pixelH, true);
        }
    }
}
