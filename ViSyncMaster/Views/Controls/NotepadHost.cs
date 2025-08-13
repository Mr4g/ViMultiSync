using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViSyncMaster.Controls
{
    public class NotepadHost : NativeControlHost
    {
        private Process? _process;
        private IntPtr _childHwnd;
        private IDisposable? _boundsSub;

        // ⬇️ TU: zapamiętujemy uchwyt hosta
        private IPlatformHandle? _hostHandle;

        private const int GWL_STYLE = -16;
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        // x64-safe warianty
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _process = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true });
            if (_process is null) return;

            // Poczekaj na okno
            await Task.Run(() =>
            {
                _process.WaitForInputIdle(5000);
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 10000)
                {
                    _process.Refresh();
                    if (_process.MainWindowHandle != IntPtr.Zero)
                    {
                        _childHwnd = _process.MainWindowHandle;
                        break;
                    }
                    Thread.Sleep(50);
                }
            });

            if (_childHwnd == IntPtr.Zero) return;

            _boundsSub = this.GetObservable(BoundsProperty)
                              .Throttle(TimeSpan.FromMilliseconds(16))
                              .Subscribe(_ => UpdateChildBounds());

            // Spróbuj podpiąć, jeśli host już istnieje
            TryAttachToCurrentHost();
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            // Zwracamy natywny host od Avalonia i ZAPISUJEMY jego uchwyt
            var handle = base.CreateNativeControlCore(parent);
            _hostHandle = handle;                 // ⬅️ kluczowe
            TryAttachToCurrentHost();             // jeśli dziecko już gotowe, podepnij teraz
            return handle;
        }

        private void TryAttachToCurrentHost()
        {
            if (_childHwnd == IntPtr.Zero || _hostHandle is null)
                return;

            // Usuń ramki i ustaw WS_CHILD|WS_VISIBLE (x64-safe)
            var style = (nuint)GetWindowLongPtr(_childHwnd, GWL_STYLE);
            style = (style | WS_CHILD | WS_VISIBLE) & ~(WS_BORDER | WS_CAPTION);
            SetWindowLongPtr(_childHwnd, GWL_STYLE, (nint)style);

            // Reparent do uchwytu hosta
            SetParent(_childHwnd, _hostHandle.Handle);

            UpdateChildBounds();
        }

        private void UpdateChildBounds()
        {
            if (_childHwnd == IntPtr.Zero || _hostHandle is null)
                return;

            var b = Bounds;
            var scale = (this.GetVisualRoot() as IRenderRoot)?.RenderScaling ?? 1.0;
            var w = Math.Max(1, (int)Math.Round(b.Width * scale));
            var h = Math.Max(1, (int)Math.Round(b.Height * scale));

            MoveWindow(_childHwnd, 0, 0, w, h, true);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            base.DestroyNativeControlCore(control);

            _boundsSub?.Dispose();
            _boundsSub = null;

            try
            {
                if (_process is { HasExited: false })
                {
                    // grzeczne zamknięcie:
                    // SendMessage(_childHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); // jeśli dodasz P/Invoke do SendMessage
                    _process.CloseMainWindow();
                    _process.WaitForExit(2000);
                    if (!_process.HasExited)
                        _process.Kill(true);
                }
            }
            catch { /* ignore */ }

            _process?.Dispose();
            _process = null;
            _childHwnd = IntPtr.Zero;
            _hostHandle = null;   // ⬅️ wyczyść referencję
        }
    }
}
