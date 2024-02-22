using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ViMultiSync.DataModel;
using System.Reflection;

namespace ViMultiSync.Services
{
    public class DummyStatusInterfaceService : IStatusInterfaceService
    {
        private const string ResourceNamespace = "ViMultiSync.Resources";
        private const string DowntimePanelFileName = "DowntimePanelData.json";

        private async Task<List<T>> ReadPanelDataAsync<T>(string fileName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);

            if (File.Exists(filePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<List<T>>(jsonData) ?? new List<T>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd odczytu pliku konfiguracyjnego: {ex.Message}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"Plik konfiguracyjny nie istnieje: {filePath}");
                return null;
            }
        }

        public async Task<List<DowntimePanelItem>> GetDowntimePanelAsync()
        {
            return await ReadPanelDataAsync<DowntimePanelItem>(DowntimePanelFileName);
        }

        public async Task<List<SettingPanelItem>> GetSettingPanelAsync()
        {
            return await ReadPanelDataAsync<SettingPanelItem>("SettingPanelData.json");
        }

        public async Task<List<MaintenancePanelItem>> GetMaintenancePanelAsync()
        {
            return await ReadPanelDataAsync<MaintenancePanelItem>("MaintenancePanelData.json");
        }

        public async Task<List<LogisticPanelItem>> GetLogisticPanelAsync()
        {
            return await ReadPanelDataAsync<LogisticPanelItem>("LogisticPanelData.json");
        }

        public async Task<List<ReasonDowntimePanelItem>> GetReasonDowntimePanelAsync()
        {
            return await ReadPanelDataAsync<ReasonDowntimePanelItem>("ReasonDowntimePanelData.json");
        }

        public async Task<List<SplunkPanelItem>> GetSplunkPanelAsync()
        {
            return await ReadPanelDataAsync<SplunkPanelItem>("SplunkPanelData.json");
        }

        public async Task<List<CallForServicePanelItem>> GetCallForServicePanelAsync()
        {
            return await ReadPanelDataAsync<CallForServicePanelItem>("CallForServicePanelData.json");
        }

        public async Task<List<ServiceArrivalPanelItem>> GetServiceArrivalPanelAsync()
        {
            return await ReadPanelDataAsync<ServiceArrivalPanelItem>("ServiceArrivalPanelData.json");
        }

        public async Task<List<DowntimeReasonElectricPanelItem>> GetDowntimeReasonElectricPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonElectricPanelItem>("DowntimeReasonElectricPanelData.json");
        }

        public async Task<List<DowntimeReasonSettingPanelItem>> GetDowntimeReasonSettingPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonSettingPanelItem>("DowntimeReasonSettingPanelData.json");
        }

        public async Task<List<DowntimeReasonKptjPanelItem>> GetDowntimeReasonKptjPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonKptjPanelItem>("DowntimeReasonKptjPanelData.json");
        }
    }
}
