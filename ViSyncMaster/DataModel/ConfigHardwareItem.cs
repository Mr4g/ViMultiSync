using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class ConfigHardwareItem
    {
        public string? SerialNumber { get; set; }
        public string? Hostname { get; set; }
        public string? AnyDeskId { get; set; }
        public string? LinkToManual { get; set; }
        public string VersionApp { get; private set; } = "1.03";
    }
}
