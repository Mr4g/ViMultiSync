using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class ProducingMessage : MachineStatus
    {
        public ProducingMessage()
        {
            this.Name = "S1.Producing_PG";
        }
    }


}