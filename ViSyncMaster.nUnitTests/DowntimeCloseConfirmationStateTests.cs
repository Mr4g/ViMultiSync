using NUnit.Framework;
using System;
using ViSyncMaster.DataModel;
using ViSyncMaster.DeepCopy;
using ViSyncMaster.Services;

namespace ViSyncMaster.nUnitTests
{
    [TestFixture]
    public class DowntimeCloseConfirmationStateTests
    {
        private static readonly string[] DowntimeNames =
        {
            "S1.LogisticMode_IPC",
            "S1.ProductionIssuesMode_IPC",
            "S1.SettingMode_IPC",
            "S1.MachineDowntime_IPC"
        };

        [Test]
        public void StartCloseThenHeartbeatSnapshotAndNewStart_StopsOldCloseConfirmation()
        {
            var state = new DowntimeCloseConfirmationState(DowntimeNames);

            var downtimeStart = new MachineStatus
            {
                Id = 1001,
                Name = "S1.MachineDowntime_IPC",
                Status = "AWARIA START",
                StartTime = DateTime.Now
            };

            Assert.That(state.IsDowntimeStatus(downtimeStart), Is.True);
            Assert.That(state.TryGetLastClosed(out _), Is.False);

            var downtimeClose = downtimeStart.DeepCopy();
            downtimeClose.EndTime = DateTime.Now.AddMinutes(5);
            downtimeClose.Status = "AWARIA CLOSE";
            state.RegisterClosed(downtimeClose, stopsLine: true);

            Assert.That(state.TryGetLastClosed(out var snapshotAfterClose), Is.True);
            Assert.That(snapshotAfterClose.StatusSnapshot.Id, Is.EqualTo(downtimeClose.Id));
            Assert.That(snapshotAfterClose.StopsLine, Is.True);
            Assert.That(snapshotAfterClose.StatusSnapshot.EndTime.HasValue, Is.True);

            // Symulacja "po 15 min republish close":
            // scheduler korzysta z tego samego snapshotu, więc oczekujemy, że dalej istnieje.
            Assert.That(state.TryGetLastClosed(out var snapshotAfter15Min), Is.True);
            Assert.That(snapshotAfter15Min.StatusSnapshot.Id, Is.EqualTo(downtimeClose.Id));

            var nextDowntimeStart = new MachineStatus
            {
                Id = 2002,
                Name = "S1.MachineDowntime_IPC",
                Status = "NOWA AWARIA",
                StartTime = DateTime.Now.AddMinutes(16)
            };

            var cleared = state.ClearForActiveDowntime();
            Assert.That(cleared, Is.True);
            Assert.That(state.TryGetLastClosed(out _), Is.False);
            Assert.That(state.IsDowntimeStatus(nextDowntimeStart), Is.True);
        }
    }
}
