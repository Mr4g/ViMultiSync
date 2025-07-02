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
        Task<List<ProductionIssuesPanelItem>> GetProductionIssuesPanelAsync();
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
        Task<List<DowntimeReasonBindownicaPanelItem>> GetDowntimeReasonBindownicaPanelAsync();   
        Task<List<DowntimeReasonSC200PanelItem>> GetDowntimeReasonSC200PanelAsync();             
        Task<List<DowntimeReasonZebraPanelItem>> GetDowntimeReasonZebraPanelAsync();             
        Task<List<DowntimeReasonWtryskarkaPanelItem>> GetDowntimeReasonWtryskarkaPanelAsync();   
        Task<List<DowntimeReasonBradyPanelItem>> GetDowntimeReasonBradyPanelAsync();             
        Task<List<DowntimeReasonZFPanelItem>> GetDowntimeReasonZFPanelAsync();                   
        Task<List<DowntimeReasonWiazarkaPanelItem>> GetDowntimeReasonWiazarkaPanelAsync();       
        Task<List<DowntimeReasonKomaxPanelItem>> GetDowntimeReasonKomaxPanelAsync();             
        Task<List<DowntimeReasonURPanelItem>> GetDowntimeReasonURPanelAsync();                   
        Task<List<DowntimeReasonDozownikPanelItem>> GetDowntimeReasonDozownikPanelAsync();       
        Task<List<DowntimeReasonWalcarkaPanelItem>> GetDowntimeReasonWalcarkaPanelAsync();       
        Task<List<DowntimeReasonTesterWodnyPanelItem>> GetDowntimeReasonTesterWodnyPanelAsync(); 
        Task<List<DowntimeReasonTesterPanelItem>> GetDowntimeReasonTesterPanelAsync();
    }
}
