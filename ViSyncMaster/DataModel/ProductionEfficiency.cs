using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class ProductionEfficiency : IEntity
    {
        public ProductionEfficiency()
        {
            this.Name = "S7.ProductionEfficiency";
        }

        public string? Name { get; set; }
        public long? SendTime { get; set; }
        public string SendStatus { get; set; } = "Pending";
        public double? Efficiency { get; set; }
        public double? EfficiencyRequired { get; set;}
        public double? Target { get; set; } 
        public int? PassedPiecesPerShift { get; set; }
        public int? FailedPiecesPerShift { get; set; }
        public long Id { get; set; }
    }
}