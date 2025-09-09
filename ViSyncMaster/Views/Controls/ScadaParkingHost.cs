using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using System;
using System.Reactive.Linq;
using ViSyncMaster.Services;

namespace ViSyncMaster.Views.Controls
{
    /// <summary>
    /// Niewidoczny host trzymany stale w drzewie – „parking” dla okna SCADA.
    /// </summary>
    public class ScadaParkingHost : NativeControlHost
    {
        public static IntPtr CurrentHandle { get; private set; } = IntPtr.Zero;

        private IDisposable? _boundsSub;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var h = base.CreateNativeControlCore(parent);
            CurrentHandle = h.Handle;

            // po starcie aplikacji przypnij tutaj okno, dopóki nikt inny go nie potrzebuje
            _ = AttachHereAsync();

            // Aktualizuj rozmiar (mały, ale nie 0×0)
            _boundsSub ??= this.GetObservable(BoundsProperty)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Subscribe(_ => UpdateBounds());

            return h;
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            base.DestroyNativeControlCore(control);
            _boundsSub?.Dispose();
            _boundsSub = null;
            CurrentHandle = IntPtr.Zero;
        }

        private async System.Threading.Tasks.Task AttachHereAsync()
        {
            var sharedDataService = new SharedDataService();
                        if (sharedDataService.AppConfig?.AppMode != "ODUSCADA")
                            return;
            
            var ok = await ScadaProcessManager.Instance.EnsureStartedAsync();
                        if (!ok) return;
            var(w, h) = GetPixelSize();
            ScadaProcessManager.Instance.AttachToHost(CurrentHandle, w, h);
        }

        private void UpdateBounds()
        {
            var (w, h) = GetPixelSize();
            if (CurrentHandle != IntPtr.Zero)
                ScadaProcessManager.Instance.AttachToHost(CurrentHandle, w, h);
        }

        private (int w, int h) GetPixelSize()
        {
            var scale = (this.GetVisualRoot() as IRenderRoot)?.RenderScaling ?? 1.0;
            // „kieszonkowy” rozmiar – nie 0×0, żeby MoveWindow miało sens
            int w = Math.Max(1, (int)Math.Round(100 * scale));
            int h = Math.Max(1, (int)Math.Round(80 * scale));
            return (w, h);
        }
    }
}
