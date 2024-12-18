using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class ConnectedMessage : MachineStatus
    {
        public ConnectedMessage()
        {
            this.Name = "S1.Connected";
        }
    }


}