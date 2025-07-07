using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using System.Reflection;
using System.Linq;

namespace ViSyncMaster.Services
{
    public class DummyStatusInterfaceService : IStatusInterfaceService
    {
        //private const string ResourceNamespace = "ViSyncMaster.Resources";
        //private const string DowntimePanelFileName = "DowntimePanelData.json";
        private string Hostname = Environment.MachineName;

        private async Task<List<T>> ReadPanelDataAsync<T>(string sectionName)
        {
            string configFilePath = Path.Combine("C:", "ViSM", "ConfigFiles", $"{Hostname}.json");

            if (File.Exists(configFilePath))
            {
                try
                {
                    string jsonData = await File.ReadAllTextAsync(configFilePath);
                    var jsonDoc = JsonDocument.Parse(jsonData);

                    // Sprawdź, czy sekcja istnieje
                    if (jsonDoc.RootElement.TryGetProperty(sectionName, out JsonElement sectionElement) && sectionElement.ValueKind == JsonValueKind.Array)
                    {
                        // Parsuj dane z sekcji
                        return sectionElement
                            .EnumerateArray()
                            .Select(item => JsonSerializer.Deserialize<T>(item.GetRawText()))
                            .ToList();
                    }
                    else
                    {
                        Console.WriteLine($"Sekcja {sectionName} nie została znaleziona w pliku konfiguracyjnym.");
                        return new List<T>(); // Zwróć pustą listę, jeśli sekcja nie istnieje
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd odczytu sekcji {sectionName} w pliku konfiguracyjnym: {ex.Message}");
                    return new List<T>(); // Zwróć pustą listę w przypadku błędu
                }
            }
            else
            {
                Console.WriteLine($"Plik konfiguracyjny nie istnieje: {configFilePath}");
                return new List<T>(); // Zwróć pustą listę, jeśli plik nie istnieje
            }
        }

        private async Task<List<T>> ReadResourcesDataAsync<T>(string fileName)
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
            return await ReadPanelDataAsync<DowntimePanelItem>("DowntimePanelData");
        }
        public async Task<List<PanelMapping>> GetDowntimePanelActionsAsync()
        {
            return await ReadPanelDataAsync<PanelMapping>("PanelActions");
        }

        public async Task<List<SettingPanelItem>> GetSettingPanelAsync()
        {
            return await ReadPanelDataAsync<SettingPanelItem>("SettingPanelData");
        }

        public async Task<List<MaintenancePanelItem>> GetMaintenancePanelAsync()
        {
            return await ReadPanelDataAsync<MaintenancePanelItem>("MaintenancePanelData");
        }

        public async Task<List<LogisticPanelItem>> GetLogisticPanelAsync()
        {
            return await ReadPanelDataAsync<LogisticPanelItem>("LogisticPanelData");
        }

        public async Task<List<ProductionIssuesPanelItem>> GetProductionIssuesPanelAsync()
        {
            return await ReadPanelDataAsync<ProductionIssuesPanelItem>("ProductionIssuesPanelData");
        }

        public async Task<List<ReasonDowntimeMechanicalPanelItem>> GetReasonDowntimeMechanicalPanelAsync()
        {
            return await ReadPanelDataAsync<ReasonDowntimeMechanicalPanelItem>("ReasonDowntimeMechanicalPanelData");
        }

        public async Task<List<SplunkPanelItem>> GetSplunkPanelAsync()
        {
            return await ReadPanelDataAsync<SplunkPanelItem>("SplunkPanelData");
        }

        public async Task<List<CallForServicePanelItem>> GetCallForServicePanelAsync()
        {
            return await ReadPanelDataAsync<CallForServicePanelItem>("CallForServicePanelData");
        }

        public async Task<List<ServiceArrivalPanelItem>> GetServiceArrivalPanelAsync()
        {
            return await ReadPanelDataAsync<ServiceArrivalPanelItem>("ServiceArrivalPanelData");
        }

        public async Task<List<DowntimeReasonElectricPanelItem>> GetDowntimeReasonElectricPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonElectricPanelItem>("DowntimeReasonElectricPanelData");
        }

        public async Task<List<DowntimeReasonLiderPanelItem>> GetDowntimeReasonLiderPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonLiderPanelItem>("DowntimeReasonLiderPanelData");
        }

        public async Task<List<DowntimeReasonKptjPanelItem>> GetDowntimeReasonKptjPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonKptjPanelItem>("DowntimeReasonKptjPanelData");
        }

        public async Task<List<DowntimeReasonPlatePanelItem>> GetDowntimeReasonPlatePanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonPlatePanelItem>("DowntimeReasonPlatePanelData");
        }
        public async Task<List<ConfigMqtt>> GetConfigMqttAsync()
        {
            return await ReadPanelDataAsync<ConfigMqtt>("ConfigMqtt");
        }
        public async Task<List<ConfigHardwareItem>> GetConfigHardwareAsync()
        {
            return await ReadResourcesDataAsync<ConfigHardwareItem>("ConfigHardware.json");
        }

        public async Task<List<DowntimeReasonBindownicaPanelItem>> GetDowntimeReasonBindownicaPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonBindownicaPanelItem>("DowntimeReasonBindownicaPanelData");
        }
        public async Task<List<DowntimeReasonSC200PanelItem>> GetDowntimeReasonSC200PanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonSC200PanelItem>("DowntimeReasonSC200PanelData");
        }
        public async Task<List<DowntimeReasonZebraPanelItem>> GetDowntimeReasonZebraPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonZebraPanelItem>("DowntimeReasonZebraPanelData");
        }
        public async Task<List<DowntimeReasonWtryskarkaPanelItem>> GetDowntimeReasonWtryskarkaPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonWtryskarkaPanelItem>("DowntimeReasonWtryskarkaPanelData");
        }
        public async Task<List<DowntimeReasonBradyPanelItem>> GetDowntimeReasonBradyPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonBradyPanelItem>("DowntimeReasonBradyPanelData");
        }
        public async Task<List<DowntimeReasonZFPanelItem>> GetDowntimeReasonZFPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonZFPanelItem>("DowntimeReasonZFPanelData");
        }
        public async Task<List<DowntimeReasonWiazarkaPanelItem>> GetDowntimeReasonWiazarkaPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonWiazarkaPanelItem>("DowntimeReasonWiazarkaPanelData");
        }
        public async Task<List<DowntimeReasonKomaxPanelItem>> GetDowntimeReasonKomaxPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonKomaxPanelItem>("DowntimeReasonKomaxPanelData");
        }
        public async Task<List<DowntimeReasonURPanelItem>> GetDowntimeReasonURPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonURPanelItem>("DowntimeReasonURPanelData");
        }
        public async Task<List<DowntimeReasonDozownikPanelItem>> GetDowntimeReasonDozownikPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonDozownikPanelItem>("DowntimeReasonDozownikPanelData");
        }
        public async Task<List<DowntimeReasonWalcarkaPanelItem>> GetDowntimeReasonWalcarkaPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonWalcarkaPanelItem>("DowntimeReasonWalcarkaPanelData");
        }
        public async Task<List<DowntimeReasonTesterWodnyPanelItem>> GetDowntimeReasonTesterWodnyPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonTesterWodnyPanelItem>("DowntimeReasonTesterWodnyPanelData");
        }
        public async Task<List<DowntimeReasonTesterPanelItem>> GetDowntimeReasonTesterPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonTesterPanelItem>("DowntimeReasonTesterPanelData");
        }
        public async Task<List<DowntimeReasonLumbergPanelItem>> GetDowntimeReasonLumbergPanelAsync()
        {
            return await ReadPanelDataAsync<DowntimeReasonLumbergPanelItem>("DowntimeReasonLumbergPanelData");
        }
    }
}
