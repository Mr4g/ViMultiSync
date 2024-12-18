using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Entitys
{
    public interface IEntity
    {
        long Id { get; set; } 
        string Value { get; }
        public string Name { get; set; }
        public string Status { get; set; }
        string Reason { get; set; }
    }
}
