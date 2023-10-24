using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.TextFormatting;

namespace ViMultiSync.DataModel
{

    /// <summary>
    /// Information about a chanel configuration
    /// </summary>
    public class DowntimePanelItem
    {
        public string Status { get; init; }

        public string LongText { get; init; }

        public string ShortText { get; init; }

    }
}
