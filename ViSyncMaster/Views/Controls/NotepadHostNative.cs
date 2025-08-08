using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ViSyncMaster.Views.Controls
{
    public class NotepadHostNative : NativeControlHost
    {
        private Process? _process;
        private IntPtr _childHwnd;

        private const int GWL_STYLE = -16;
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _process = Process.Start("notepad.exe");
            if (_process == null)
                return;

            _process.WaitForInputIdle();
            _childHwnd = _process.MainWindowHandle;

            LayoutUpdated += (_, __) =>
            {
                if (_childHwnd != IntPtr.Zero)
                {
                    var b = Bounds;
                    MoveWindow(_childHwnd, 0, 0, (int)b.Width, (int)b.Height, true);
                }
            };
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var parentHwnd = parent.Handle;

            var style = GetWindowLong(_childHwnd, GWL_STYLE);
            style = (style | WS_CHILD | WS_VISIBLE) & ~(WS_BORDER | WS_CAPTION);
            SetWindowLong(_childHwnd, GWL_STYLE, style);

            SetParent(_childHwnd, parentHwnd);

            var b = Bounds;
            MoveWindow(_childHwnd, 0, 0, (int)b.Width, (int)b.Height, true);

            return new PlatformHandle(_childHwnd, "HWND");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (_process is { HasExited: false })
                _process.Kill();
            _process?.Dispose();
        }
    }
}
