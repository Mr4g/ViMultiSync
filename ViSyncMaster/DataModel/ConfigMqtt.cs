using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class ConfigMqtt
    {
        public string brokerHost { get; set; }
        public int brokerPort { get; set; }
        public string clientId { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string topic { get; set; }  
    }
}
