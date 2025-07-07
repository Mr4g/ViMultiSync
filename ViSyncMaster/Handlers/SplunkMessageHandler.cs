using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Handlers
{
    public class SplunkMessageHandler
    {
        private MessagePgToSplunk _messageToSplunkPg;
        private int _singleStatus;
        public object PreparingPgMessageToSplunk(ObservableCollection<MachineStatus> machineStatuses, int machineStatusCounter)
        {

            if (machineStatuses != null && machineStatuses.Any())
            {
                var downtimeStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.MachineDowntime_IPC" && ms.IsActive == true);
                var maintenanceStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.MaintenanceMode_IPC" && ms.IsActive == true);
                var settingStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.SettingMode_IPC" && ms.IsActive == true);
                var producingStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.Producing_PG" && ms.IsActive == true);
                var logisticStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.LogisticMode_IPC" && ms.IsActive == true);
                var waitingStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.Waiting_PG" && ms.IsActive == true);

                if (downtimeStatus != null)
                    machineStatusCounter = 1;
                else if (logisticStatus != null)
                    machineStatusCounter = 2;
                else if (settingStatus != null)
                    machineStatusCounter = 3;
                else if (maintenanceStatus != null)
                    machineStatusCounter = 4;
                else if (producingStatus != null)
                    machineStatusCounter = 5;
                else if (waitingStatus != null)
                    machineStatusCounter = 6;
            }

            _messageToSplunkPg.SetByCounter(machineStatusCounter);
            return _messageToSplunkPg;
        }
        public object PreparingPgMessageToSplunk(ObservableCollection<MachineStatus> machineStatuses, MachineStatus machineStatus, int machineStatusCounter)
        {
            int newStatus = machineStatusCounter; // Przechowujemy aktualny status

            IEnumerable<MachineStatus> statusesForEvaluation = machineStatuses ?? Enumerable.Empty<MachineStatus>();

            // Jeśli przekazany status został zakończony, kolekcja może nie być jeszcze odświeżona
            if (machineStatus != null && !machineStatus.IsActive)
                statusesForEvaluation = statusesForEvaluation.Where(ms => ms.Id != machineStatus.Id);

            // Priorytetizacja na podstawie kolekcji
            if (statusesForEvaluation.Any())
            {
                if (statusesForEvaluation.Any(ms => ms.Name == "S1.MachineDowntime_IPC" && ms.IsActive))
                    newStatus = 1;
                else if (statusesForEvaluation.Any(ms => ms.Name == "S1.LogisticMode_IPC" && ms.IsActive))
                    newStatus = 2;
                else if (statusesForEvaluation.Any(ms => ms.Name == "S1.SettingMode_IPC" && ms.IsActive))
                    newStatus = 3;
                else if (statusesForEvaluation.Any(ms => ms.Name == "S1.MaintenanceMode_IPC" && ms.IsActive))
                    newStatus = 4;
                else if (statusesForEvaluation.Any(ms => ms.Name == "S1.Producing_PG" && ms.IsActive))
                    newStatus = 5;
                else if (statusesForEvaluation.Any(ms => ms.Name == "S1.Waiting_PG" && ms.IsActive))
                    newStatus = 6;
            }

            // Priorytetizacja na podstawie pojedynczego statusu
            if (machineStatus != null && machineStatus.IsActive)
            {
                int singleStatus = newStatus; // Kopia obecnego statusu

                if (machineStatus.Name == "S1.MachineDowntime_IPC")
                    singleStatus = 1;
                else if (machineStatus.Name == "S1.LogisticMode_IPC")
                    singleStatus = 2;
                else if (machineStatus.Name == "S1.SettingMode_IPC")
                    singleStatus = 3;
                else if (machineStatus.Name == "S1.MaintenanceMode_IPC")
                    singleStatus = 4;
                else if (machineStatus.Name == "S1.Producing_PG")
                    singleStatus = 5;
                else if (machineStatus.Name == "S1.Waiting_PG")
                    singleStatus = 6;

                // Aktualizujemy tylko, jeśli pojedynczy status ma wyższy priorytet
                if (singleStatus < newStatus || newStatus == 0)
                {
                    newStatus = singleStatus;
                }
            }
            bool anyStillActive = statusesForEvaluation.Any(ms => ms.IsActive);
            if (machineStatus != null && !machineStatus.IsActive && !anyStillActive)
            {
                newStatus = 6;
            }

            // Ustawienie statusu w obiekcie Splunk
            _messageToSplunkPg.SetByCounter(newStatus);
            return _messageToSplunkPg;
        }

        public SplunkMessageHandler()
        {
            _messageToSplunkPg = new MessagePgToSplunk();
        }
    }
}
