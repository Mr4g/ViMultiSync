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
        Task<List<SettingPanelItem>> GetSettingPanelAsync();

        Task<List<MaintenancePanelItem>> GetMaintenancePanelAsync();
        Task<List<LogisticPanelItem>> GetLogisticPanelAsync();
        Task<List<ReasonDowntimePanelItem>> GetReasonDowntimePanelAsync();
        Task<List<SplunkPanelItem>> GetSplunkPanelAsync();
    }
}
