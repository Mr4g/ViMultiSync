using System;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class HourlyPlanMessage : IEntity
    {
        public HourlyPlanMessage()
        {
            Name = "S7.HourlyPlan";
        }

        public string? Name { get; set; }
        public long? SendTime { get; set; }
        public string SendStatus { get; set; } = "Pending";

        public long Id { get; set; }

        public string Period { get; set; }
        public int ExpectedUnits { get; set; }
        public int ProducedUnits { get; set; }
        public int DowntimeMinutes { get; set; }
        public int Total { get; set; }

        public bool IsBreak { get; set; }
        public bool IsBreakActive { get; set; }
        public int LostUnitsDueToDowntime { get; set; }
        public double Efficiency { get; set; }
        public int RetryCount { get; set; } = 0;
        public string? LastAttemptAt { get; set; }
        public string? LastError { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
