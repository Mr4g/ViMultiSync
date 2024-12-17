using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class MessagePgToSplunk 
    {
        public string Producing { get; set; }
        public string Waiting { get; set; }
        public string Maintenace { get; set; }
        public string Setting { get; set; }
        public string Downtime { get; set; }

      
        public MessagePgToSplunk() 
        {
            this.Producing = "false";
            this.Waiting = "true";
            this.Maintenace = "false";
            this.Downtime = "false";
            this.Setting = "false";
        }
    }

}
