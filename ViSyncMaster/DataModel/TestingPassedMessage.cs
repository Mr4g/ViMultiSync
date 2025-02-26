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

        public string? ProductName { get; set; } 
        public string? OperatorId { get; set; }
        private bool _isActive = false;
        private TimeSpan? _durationStatus = null; 
        public override string Value => _value;

        public override bool IsActive => _isActive;

        public override TimeSpan? DurationStatus => _durationStatus;

        public void SetValue(string value)
        {
            _value = value;
        }
    }


}