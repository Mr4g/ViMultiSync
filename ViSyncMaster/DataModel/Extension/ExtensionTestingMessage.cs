using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel.Extension
{
    public static class ExtensionTestingMessage
    {
        public static Dictionary<string, object> ToMqttFormatForCuntersMessage(this MachineCounters status)
        {
            return new Dictionary<string, object>
            { 
                { "Counters", status }
            };
        }
    }
}
