using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class StatusModel
    {
        public ButtonStatus ActualStatus { get; set; }
        public ButtonStatus CallForService { get; set; }
        public ButtonStatus ServiceArrival { get; set; }
    }
    public class ButtonStatus
    {
        public string Text { get; set; }
        public string Color { get; set; }
        public string Timer { get; set; } // Opcjonalnie, dla przycisków z timerem
    }
}
