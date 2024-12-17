using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class AppConfigData
    {
        public string Index { get; set; }
        public string Source { get; set; }
        public string TokenSplunk { get; set; }
        public string UrlSplunk { get; set; }
        public string UrlSap { get; set; }
        public string UrlAdaptronic { get; set; }
        public string UrlDiscGoogle { get; set; }
        public string UrlInstruction { get; set; }
        public string UrlTargetPlan { get; set; }
        public string IpTesterKt { get; set; }
        public string Hostname { get; set; }
        public string Workplace { get; set; }
        public string IsMachine { get; set; }
        public bool VacuumPanelAvailable { get; set; }
        public int RemainingVacuumTimeDefault { get; set; } 
        public string Line { get; set; }
        public string WorkplaceName { get; set; }
        public string SpanForKeepAlive { get; set; }
        public bool LoginToApp { get; set; }
        public string ComNumber { get; set; }
        public string AppMode { get; set; }
    }
}
