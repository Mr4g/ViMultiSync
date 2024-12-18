using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.TextFormatting;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{

    /// <summary>
    /// Information about a chanel configuration
    /// </summary>
    public class DowntimePanelItem : MachineStatus
    {
        public DowntimePanelItem()
        {
            this.Color = "#DC4E41";
        }
    }
}
