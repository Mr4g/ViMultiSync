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