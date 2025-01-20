using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class TestingPassedMessage : MachineStatus
    {
        public TestingPassedMessage()
        {
            this.Name = "S7.TestingPassed";
        }
        private string _value = "false";
        public override string Value => _value;

        public void SetValue(string value)
        {
            _value = value;
        }
    }


}