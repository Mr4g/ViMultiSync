using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Handlers;
using ViSyncMaster.ViewModels;

namespace ViSyncMaster.nUnitTests
{
    [TestFixture]
    public class SplunkMessageHandlerTests
    {
        private SplunkMessageHandler _handler;

        [Test]
        public void PreparingPgMessageToSplunk_DowntimeStatusTrue_SetsCounterTo5()
        {
            // Arrange
            var _handler = new SplunkMessageHandler();
            var machineStatuses = new ObservableCollection<MachineStatus> // Pusta lista       
            {
                new MachineStatus 
                {
                Name = "S1.MachineDowntime_IPC",
                Status = "ELEKTRYCZNA",
                StartTime = DateTime.Now
                }
            };

            int initialCounter = 3;

            // Act
            var result = _handler.PreparingPgMessageToSplunk(machineStatuses, initialCounter) as MessagePgToSplunk;

            // Assert
            Assert.AreEqual("false", result.Producing);
            Assert.AreEqual("false", result.Waiting);
            Assert.AreEqual("false", result.Maintenance);
            Assert.AreEqual("false", result.Setting);
            Assert.AreEqual("true", result.Downtime);

        }

        [Test]
        public void PreparingPgMessageToSplunk_ProducingStatusTrue_SetsCounterTo1()
        {
            // Arrange
            var _handler = new SplunkMessageHandler();
            var machineStatuses = new ObservableCollection<MachineStatus>();
    

            int initialCounter = 1;

            // Act
            var result = _handler.PreparingPgMessageToSplunk(machineStatuses, initialCounter) as MessagePgToSplunk;

            // Assert
            Assert.AreEqual("true", result.Producing);
            Assert.AreEqual("false", result.Waiting);
            Assert.AreEqual("false", result.Maintenance);
            Assert.AreEqual("false", result.Setting);
            Assert.AreEqual("false", result.Downtime);
        }

        [Test]
        public void PreparingPgMessageToSplunk_WaitingStatusTrue_SetsCounterTo2()
        {
            // Arrange
            var _handler = new SplunkMessageHandler();
            var machineStatuses = new ObservableCollection<MachineStatus>
    {
        new MachineStatus
        {
            Name = "S1.MaintenanceMode_IPC",
            Status = "ELEKTRYCZNA",
            StartTime = DateTime.Now
        },
        new MachineStatus
        {
            Name = "S1.SettingMode_IPC",
            Status = "ELEKTRYCZNA",
            StartTime = DateTime.Now
        },
        new MachineStatus
        {
            Name = "S1.MachineDowntime_IPC",
            Status = "ELEKTRYCZNA",
            StartTime = DateTime.Now
        }
    };

            int initialCounter = 2;

            // Act
            var result = _handler.PreparingPgMessageToSplunk(machineStatuses, initialCounter) as MessagePgToSplunk;

            // Assert
            Assert.AreEqual("false", result.Producing);
            Assert.AreEqual("false", result.Waiting);
            Assert.AreEqual("false", result.Maintenance);
            Assert.AreEqual("false", result.Setting);
            Assert.AreEqual("true", result.Downtime);
        }

        [Test]
        public void PreparingPgMessageToSplunk_MaintenanceStatusTrue_SetsCounterTo3()
        {
            // Arrange
            var _handler = new SplunkMessageHandler();
            var machineStatuses = new ObservableCollection<MachineStatus>
    {
        new MachineStatus
        {
            Name = "S1.MaintenanceMode_IPC",
            Status = "ELEKTRYCZNA",
            StartTime = DateTime.Now
        }
    };

            int initialCounter = 0;

            // Act
            var result = _handler.PreparingPgMessageToSplunk(machineStatuses, initialCounter) as MessagePgToSplunk;

            // Assert
            Assert.AreEqual("false", result.Producing);
            Assert.AreEqual("false", result.Waiting);
            Assert.AreEqual("true", result.Maintenance);
            Assert.AreEqual("false", result.Setting);
            Assert.AreEqual("false", result.Downtime);
        }

        [Test]
        public void PreparingPgMessageToSplunk_SettingStatusTrue_SetsCounterTo4()
        {
            // Arrange
            var _handler = new SplunkMessageHandler();
            var machineStatuses = new ObservableCollection<MachineStatus>
    {
        new MachineStatus
        {
            Name = "S1.SettingMode_IPC",
            Status = "ELEKTRYCZNA",
            StartTime = DateTime.Now
        }
    };

            int initialCounter = 5;

            // Act
            var result = _handler.PreparingPgMessageToSplunk(machineStatuses, initialCounter) as MessagePgToSplunk;

            // Assert
            Assert.AreEqual("false", result.Producing);
            Assert.AreEqual("false", result.Waiting);
            Assert.AreEqual("false", result.Maintenance);
            Assert.AreEqual("true", result.Setting);
            Assert.AreEqual("false", result.Downtime);
        }

        [Test]
        public void PreparingPgMessageToSplunk_NoStatusTrue_SetsCounterTo2()
        {
            // Arrange
            var _handler = new SplunkMessageHandler();
            var machineStatuses = new ObservableCollection<MachineStatus>(); // Brak statusów

            int initialCounter = 2;

            // Act
            var result = _handler.PreparingPgMessageToSplunk(machineStatuses, initialCounter) as MessagePgToSplunk;

            // Assert
            Assert.AreEqual("false", result.Producing);
            Assert.AreEqual("true", result.Waiting);
            Assert.AreEqual("false", result.Maintenance);
            Assert.AreEqual("false", result.Setting);
            Assert.AreEqual("false", result.Downtime);
        }
    }
}
