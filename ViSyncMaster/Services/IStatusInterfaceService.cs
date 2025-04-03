using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public interface IStatusInterfaceService
    {
        /// <summary>
        /// Fetch the channel configurations 
        /// </summary>
        /// <returns></returns>
        Task<List<DowntimePanelItem>> GetDowntimePanelAsync();
        Task<List<PanelMapping>> GetDowntimePanelActionsAsync();
        Task<List<SettingPanelItem>> GetSettingPanelAsync();
        Task<List<MaintenancePanelItem>> GetMaintenancePanelAsync();
        Task<List<LogisticPanelItem>> GetLogisticPanelAsync();
        Task<List<ReasonDowntimeMechanicalPanelItem>> GetReasonDowntimeMechanicalPanelAsync();
        Task<List<SplunkPanelItem>> GetSplunkPanelAsync();
        Task<List<CallForServicePanelItem>> GetCallForServicePanelAsync();
        Task<List<ServiceArrivalPanelItem>> GetServiceArrivalPanelAsync();
        Task<List<DowntimeReasonElectricPanelItem>> GetDowntimeReasonElectricPanelAsync();
        Task<List<DowntimeReasonLiderPanelItem>> GetDowntimeReasonLiderPanelAsync();
        Task<List<DowntimeReasonKptjPanelItem>> GetDowntimeReasonKptjPanelAsync();
        Task<List<DowntimeReasonPlatePanelItem>> GetDowntimeReasonPlatePanelAsync();
        Task<List<ConfigMqtt>> GetConfigMqttAsync();
        Task<List<ConfigHardwareItem>> GetConfigHardwareAsync();

    }
}
