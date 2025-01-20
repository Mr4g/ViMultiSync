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
        public object PreparingPgMessageToSplunk(ObservableCollection<MachineStatus> machineStatuses, int machineStatusCounter)
        {

            if (machineStatuses != null && machineStatuses.Any())
            {
                var downtimeStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.MachineDowntime_IPC" && ms.Value == "true");
                var maintenanceStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.MaintenanceMode_IPC" && ms.Value == "true");
                var settingStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.SettingMode_IPC" && ms.Value == "true");
                var producingStatus = machineStatuses.FirstOrDefault(ms => ms.Name == "S1.Producing_PG" && ms.Value == "true");

                if (downtimeStatus != null)
                    machineStatusCounter = 5;
                else if (settingStatus != null)
                    machineStatusCounter = 4;
                else if (maintenanceStatus != null)
                    machineStatusCounter = 3;
            }

            _messageToSplunkPg.SetByCounter(machineStatusCounter);
            return _messageToSplunkPg;
        }

        public SplunkMessageHandler()
        {
            _messageToSplunkPg = new MessagePgToSplunk();
        }
    }
}
