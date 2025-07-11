using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ViSyncMaster.AuxiliaryClasses
{
    public class ShiftPlansConfig
    {
        [JsonPropertyName("ShiftPlans")]
        public List<ShiftPlanEntry> ShiftPlans { get; set; }
    }

    public class ShiftPlanEntry
    {
        public string Line { get; set; } 
        public TimeSpan ShiftStart { get; set; }
        public TimeSpan ShiftEnd { get; set; }
        public TimeSpan PlanStart { get; set; }
        public TimeSpan ShutDown { get; set; }

        // this re-uses your existing record
        public List<ShiftBreak> Breaks { get; set; }
    }
}
