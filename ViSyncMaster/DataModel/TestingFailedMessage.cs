using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class TestingFailedMessage : MachineStatus
    {
        public TestingFailedMessage()
        {
            this.Name = "S7.TestingFailed";
        }
        public override string Value => "false";
    }


}