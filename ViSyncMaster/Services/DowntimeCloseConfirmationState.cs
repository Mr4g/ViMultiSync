using System;
using System.Linq;
using ViSyncMaster.DataModel;
using ViSyncMaster.DeepCopy;

namespace ViSyncMaster.Services
{
    /// <summary>
    /// Przechowuje in-memory ostatnio zamkniętą awarię i kontroluje,
    /// kiedy heartbeat zamknięcia powinien być aktywny.
    /// </summary>
    public class DowntimeCloseConfirmationState
    {
        private readonly object _sync = new();
        private readonly string[] _downtimeNames;
        private ClosedDowntimeSnapshot? _lastClosed;

        public DowntimeCloseConfirmationState(string[] downtimeNames)
        {
            _downtimeNames = downtimeNames ?? Array.Empty<string>();
        }

        public bool IsDowntimeStatus(MachineStatus status)
        {
            return status != null
                && !string.IsNullOrWhiteSpace(status.Name)
                && _downtimeNames.Contains(status.Name, StringComparer.OrdinalIgnoreCase);
        }

        public void RegisterClosed(MachineStatus status, bool stopsLine)
        {
            var snapshot = new ClosedDowntimeSnapshot(status.DeepCopy(), stopsLine, DateTime.UtcNow);
            lock (_sync)
            {
                _lastClosed = snapshot;
            }
        }

        public bool ClearForActiveDowntime()
        {
            lock (_sync)
            {
                if (_lastClosed == null)
                    return false;
                _lastClosed = null;
                return true;
            }
        }

        public bool TryGetLastClosed(out ClosedDowntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                if (_lastClosed == null)
                {
                    snapshot = null;
                    return false;
                }

                snapshot = _lastClosed with { StatusSnapshot = _lastClosed.StatusSnapshot.DeepCopy() };
                return true;
            }
        }
    }

    public record ClosedDowntimeSnapshot(MachineStatus StatusSnapshot, bool StopsLine, DateTime ClosedAtUtc);
}
