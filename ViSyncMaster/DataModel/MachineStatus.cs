using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class MachineStatus : IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public virtual string Value => EndTime.HasValue ? "false" : "true";
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsActive => !EndTime.HasValue; // Jeśli brak EndTime, status jest aktywny
        public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : DateTime.Now - StartTime;
        public string Color { get; set; }
    }
}

