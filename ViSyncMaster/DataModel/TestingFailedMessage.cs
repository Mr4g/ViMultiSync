using System;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class TestingFailedMessage : MachineStatus
    {
        public TestingFailedMessage()
        {
            this.Name = "S7.TestingFailed";
        }

        private string _value = "false";
        public override string Value => _value;

        public void SetValue(string value)
        {
            _value = value;
        }
    }
}