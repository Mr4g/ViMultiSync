using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ViSyncMaster.Services;

namespace ViSyncMaster.Views.Controls
{
    /// <summary>
    /// Widoczny host używany w stronach; przy niszczeniu przepina do ScadaParkingHost.
    /// </summary>
    public class ScadaHostNative : NativeControlHost
    {
        private IPlatformHandle? _hostHandle;
        private IDisposable? _boundsSub;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var h = base.CreateNativeControlCore(parent);
            _hostHandle = h;

            _boundsSub ??= this.GetObservable(BoundsProperty)
                .Throttle(TimeSpan.FromMilliseconds(16))
                .Subscribe(_ => UpdateAttach());

            _ = AttachAsync();
            return h;
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            // ZANIM wyjdziemy z drzewa – przepnij okno do parkingu (jeśli istnieje)
            if (_hostHandle != null && ScadaParkingHost.CurrentHandle != IntPtr.Zero)
            {
                var (w, h) = GetPixelSizeFor(ScadaParkingHost.CurrentHandle);
                ScadaProcessManager.Instance.AttachToHost(ScadaParkingHost.CurrentHandle, w, h);
            }

            _boundsSub?.Dispose();
            _boundsSub = null;
            _hostHandle = null;

            base.DestroyNativeControlCore(control);
        }

        private async Task AttachAsync()
        {
            var ok = await ScadaProcessManager.Instance.EnsureStartedAsync();
            if (!ok || _hostHandle is null) return;
            UpdateAttach();
        }

        private void UpdateAttach()
        {
            if (_hostHandle is null) return;
            var (w, h) = GetPixelSizeFor(_hostHandle.Handle);
            if (w <= 0 || h <= 0) return;

            ScadaProcessManager.Instance.AttachToHost(_hostHandle.Handle, w, h);
        }

        private (int w, int h) GetPixelSizeFor(IntPtr _)
        {
            var b = Bounds;
            var scale = (this.GetVisualRoot() as IRenderRoot)?.RenderScaling ?? 1.0;
            int w = Math.Max(1, (int)Math.Round(b.Width * scale));
            int h = Math.Max(1, (int)Math.Round(b.Height * scale));
            return (w, h);
        }
    }
}
