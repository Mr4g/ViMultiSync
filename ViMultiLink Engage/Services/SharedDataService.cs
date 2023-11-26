using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMultiSync.DataModel;

namespace ViMultiSync.Services
{
    public class SharedDataService
    {
        public AppConfigData AppConfig { get; private set; }

        private ConfigService mConifgService = new ConfigService();


        public SharedDataService()
        {
            this.LoadConfigData();
        }

        private async Task LoadConfigData()
        {
            // Pobierz AppConfigData z mConifgService
            var loadedAppConfig = await mConifgService.GetAppConfigDataAsync();

            // Przypisz do pola klasy
            this.AppConfig = loadedAppConfig;
        }
    }
}
