using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Views;

namespace ViSyncMaster.Services
{
    public class SharedDataService
    {
        public AppConfigData AppConfig { get; private set; }
        public ConfigMqtt ConfigMqtt { get; private set; }

        private ConfigService mConifgService = new ConfigService();
        private DummyStatusInterfaceService mDummyStatusInterfaceService = new DummyStatusInterfaceService();


        public SharedDataService()
        {
            Task.Run(async () => await LoadConfigData()).Wait();
        }

        private async Task LoadConfigData()
        {
            // Pobierz AppConfigData z mConifgService
            var loadedAppConfig = await mConifgService.GetAppConfigDataAsync();
            var loadedMqttConfigList = await mDummyStatusInterfaceService.GetConfigMqttAsync();

            // Przypisz do pola klasy
            this.AppConfig = loadedAppConfig ?? new AppConfigData();
            this.ConfigMqtt = loadedMqttConfigList.FirstOrDefault() ?? new ConfigMqtt();
        }
    }
}
