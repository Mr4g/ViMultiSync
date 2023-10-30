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
        private const string pathDowntimePanelData = "ViMultiSync.Resources.DowntimePanelData.json";
        private const string pathSettingPanelData = "ViMultiSync.Resources.SettingPanelData.json";
        private const string pathMaintenancePanelData = "ViMultiSync.Resources.MaintenancePanelData.json";
        private const string pathLogisticPanelData = "ViMultiSync.Resources.LogisticPanelData.json";
        private const string pathReasonDowntimePanelData = "ViMultiSync.Resources.ReasonDowntimePanelData.json";
        private const string pathSplunkPanelData = "ViMultiSync.Resources.SplunkPanelData.json";
        private const string pathServiceArrivalPanelData = "ViMultiSync.Resources.ServiceArrivalPanelData.json";



        public async Task<List<DowntimePanelItem>> GetDowntimePanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathDowntimePanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var downtimePanelItems = JsonSerializer.Deserialize<List<DowntimePanelItem>>(jsonData);
                        return downtimePanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<DowntimePanelItem>(new[]
                    {
                        new DowntimePanelItem { Name = "S1.MachineDowntime", Value = "", NameDevice = "iPC01", Status = "MECHANICZNA", Location = "ODU_5", Source = "W16_iPC_Test" },
                        new DowntimePanelItem { Name = "S1.MachineDowntime", Value = "", NameDevice = "iPC01", Status = "ELEKTRYCZNA", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new DowntimePanelItem {Name = "S1.MachineDowntime", Value = "", NameDevice = "iPC01", Status = "USTAWIACZ", Location = "ODU_5", Source = "W16_iPC_Test"},
                    }));
                }
            }
        }

        public async Task<List<SettingPanelItem>> GetSettingPanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathSettingPanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var settingPanelItems = JsonSerializer.Deserialize<List<SettingPanelItem>>(jsonData);
                        return settingPanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<SettingPanelItem>(new[]
                    {
                        new SettingPanelItem { Name = "S1.Setting", Value = "", NameDevice = "iPC01", Status = "PRZEZBROJENIE", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new SettingPanelItem { Name = "S1.Setting", Value = "", NameDevice = "iPC01", Status = "KOREKTY", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new SettingPanelItem { Name = "S1.Setting", Value = "", NameDevice = "iPC01", Status = "TESTY", Location = "ODU_5", Source = "W16_iPC_Test"},
                    }));
                }
            }
        }

        public async Task<List<MaintenancePanelItem>> GetMaintenancePanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathMaintenancePanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var maintenancePanelItems = JsonSerializer.Deserialize<List<MaintenancePanelItem>>(jsonData);
                        return maintenancePanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<MaintenancePanelItem>(new[]
                    {
                        new MaintenancePanelItem { Name = "S1.Maintenance", Value = "", NameDevice = "iPC01", Status = "SPRZĄTANIE", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new MaintenancePanelItem { Name = "S1.Maintenance", Value = "", NameDevice = "iPC01", Status = "PLANOWANA PRZERWA", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new MaintenancePanelItem { Name = "S1.Maintenance", Value = "", NameDevice = "iPC01", Status = "INNE", Location = "ODU_5", Source = "W16_iPC_Test"},
                    }));
                }
            }
        }

        public async Task<List<LogisticPanelItem>> GetLogisticPanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathLogisticPanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var logisticPanelItems = JsonSerializer.Deserialize<List<LogisticPanelItem>>(jsonData);
                        return logisticPanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<LogisticPanelItem>(new[]
                    {
                        new LogisticPanelItem { Name = "S1.Logistic", Value = "", NameDevice = "iPC01", Status = "BRAK MATERIAŁU", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new LogisticPanelItem { Name = "S1.Logistic", Value = "", NameDevice = "iPC01", Status = "USZKODZONY MATERIAŁ", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new LogisticPanelItem { Name = "S1.Logistic", Value = "", NameDevice = "iPC01", Status = "INNE", Location = "ODU_5", Source = "W16_iPC_Test"},
                    }));
                }
            }
        }

        public async Task<List<ReasonDowntimePanelItem>> GetReasonDowntimePanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathReasonDowntimePanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var reasonDowntimePanelItems = JsonSerializer.Deserialize<List<ReasonDowntimePanelItem>>(jsonData);
                        return reasonDowntimePanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<ReasonDowntimePanelItem>(new[]
                    {
                        new ReasonDowntimePanelItem { Name = "S7.ReasonDowntime", Value = "", NameDevice = "iPC01", Status = "BRAK ZASILANIA", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new ReasonDowntimePanelItem { Name = "S7.ReasonDowntime", Value = "", NameDevice = "iPC01", Status = "USZKODZONY CZUJNIK", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new ReasonDowntimePanelItem { Name = "S7.ReasonDowntime", Value = "", NameDevice = "iPC01", Status = "INNE", Location = "ODU_5", Source = "W16_iPC_Test"},
                    }));
                }
            }
        }

        public async Task<List<SplunkPanelItem>> GetSplunkPanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathSplunkPanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var splunkPanelItems = JsonSerializer.Deserialize<List<SplunkPanelItem>>(jsonData);
                        return splunkPanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<SplunkPanelItem>(new[]
                    {
                        new SplunkPanelItem { Group = "Splunk", Name = "Produkcyjny", Link = "https://splunk05w05.viessmann.net:8000/en-GB/app/VI_W16/w16_welcome"},
                        new SplunkPanelItem { Group = "Splunk", Name = "Testowy", Link = "https://w6228w05.viessmann.net:8000/en-gb/app/VI_W16/w16_welcome"},
                    }));
                }
            }
        }

        public async Task<List<ServiceArrivalPanelItem>> GetServiceArrivalPanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathServiceArrivalPanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var serviceArrivalPanelItems = JsonSerializer.Deserialize<List<ServiceArrivalPanelItem>>(jsonData);
                        return serviceArrivalPanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<ServiceArrivalPanelItem>(new[]
                    {
                        new ServiceArrivalPanelItem { Name = "S1.ServiceArrival", Value = "true", NameDevice = "iPC01", Location = "ODU_5", Source = "W16_iPC_Test"}
                    }));
                }
            }
        }
    }
}