using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMultiSync.DataModel;

namespace ViMultiSync.Services
{
    public interface IStatusInterfaceService
    {
        /// <summary>
        /// Fetch the channel configurations 
        /// </summary>
        /// <returns></returns>
        Task<List<DowntimePanelItem>> GetDowntimePanelAsync();
    }
}
