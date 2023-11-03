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
        private const string pathCallForServicePanelData = "ViMultiSync.Resources.CallForServicePanelData.json";
        private const string pathServiceArrivalPanelData = "ViMultiSync.Resources.ServiceArrivalPanelData.json";
        private const string pathDowntimeReasonElectricPanelData = "ViMultiSync.Resources.DowntimeReasonElectricPanelData.json";
        private const string pathDowntimeReasonSettingPanelData = "ViMultiSync.Resources.DowntimeReasonSettingPanelData.json";


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
                        new DowntimePanelItem { Name = "S1.MachineDowntime", Value = "true", NameDevice = "iPC01", Status = "MECHANICZNA", Location = "ODU_5", Source = "W16_iPC_Test" },
                        new DowntimePanelItem { Name = "S1.MachineDowntime", Value = "true", NameDevice = "iPC01", Status = "ELEKTRYCZNA", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new DowntimePanelItem {Name = "S1.MachineDowntime", Value = "true", NameDevice = "iPC01", Status = "USTAWIACZ", Location = "ODU_5", Source = "W16_iPC_Test"},
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
                        new SettingPanelItem { Name = "S1.SettingMode", Value = "true", NameDevice = "iPC01", Status = "PRZEZBROJENIE", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new SettingPanelItem { Name = "S1.SettingMode", Value = "true", NameDevice = "iPC01", Status = "KOREKTY", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new SettingPanelItem { Name = "S1.SettingMode", Value = "true", NameDevice = "iPC01", Status = "TESTY", Location = "ODU_5", Source = "W16_iPC_Test"},
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
                        new MaintenancePanelItem { Name = "S1.MaintenanceMode", Value = "true", NameDevice = "iPC01", Status = "SPRZĄTANIE", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new MaintenancePanelItem { Name = "S1.MaintenanceMode", Value = "true", NameDevice = "iPC01", Status = "PLANOWANA PRZERWA", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new MaintenancePanelItem { Name = "S1.MaintenanceMode", Value = "true", NameDevice = "iPC01", Status = "INNE", Location = "ODU_5", Source = "W16_iPC_Test"},
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
                        new LogisticPanelItem { Name = "S1.LogisticMode", Value = "true", NameDevice = "iPC01", Status = "BRAK MATERIAŁU", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new LogisticPanelItem { Name = "S1.LogisticMode", Value = "true", NameDevice = "iPC01", Status = "USZKODZONY MATERIAŁ", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new LogisticPanelItem { Name = "S1.LogisticMode", Value = "true", NameDevice = "iPC01", Status = "INNE", Location = "ODU_5", Source = "W16_iPC_Test"},
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
                        new ReasonDowntimePanelItem { Name = "S7.DowntimeReason", Value = "true", NameDevice = "iPC01", Status = "BRAK ZASILANIA", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new ReasonDowntimePanelItem { Name = "S7.DowntimeReason", Value = "true", NameDevice = "iPC01", Status = "USZKODZONY CZUJNIK", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new ReasonDowntimePanelItem { Name = "S7.DowntimeReason", Value = "true", NameDevice = "iPC01", Status = "INNE", Location = "ODU_5", Source = "W16_iPC_Test"},
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

        public async Task<List<CallForServicePanelItem>> GetCallForServicePanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathCallForServicePanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var callForServicePanelItems = JsonSerializer.Deserialize<List<CallForServicePanelItem>>(jsonData);
                        return callForServicePanelItems;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<CallForServicePanelItem>(new[]
                    {
                        new CallForServicePanelItem { Name = "S7.CallForService", Value = "true", NameDevice = "iPC01", Location = "ODU_5", Source = "W16_iPC_Test"},
                        new CallForServicePanelItem { Name = "S7.CallForService", Value = "false", NameDevice = "iPC01", Location = "ODU_5", Source = "W16_iPC_Test"}
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

        public async Task<List<DowntimeReasonElectricPanelItem>> GetDowntimeReasonElectricPanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathDowntimeReasonElectricPanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var downtimeReasonElectricPanelItem = JsonSerializer.Deserialize<List<DowntimeReasonElectricPanelItem>>(jsonData);
                        return downtimeReasonElectricPanelItem;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<DowntimeReasonElectricPanelItem>(new[]
                    {
                        new DowntimeReasonElectricPanelItem { Name = "S7.DowntimeReason", Value = "true", NameDevice = "iPC01", Status = "BRAK ZASILANIA", Location = "ODU_5", Source = "W16_iPC_Test"},
                    }));
                }
            }
        }
        public async Task<List<DowntimeReasonSettingPanelItem>> GetDowntimeReasonSettingPanelAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(pathDowntimeReasonSettingPanelData))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string jsonData = reader.ReadToEnd();
                        var downtimeReasonSettingPanelItem = JsonSerializer.Deserialize<List<DowntimeReasonSettingPanelItem>>(jsonData);
                        return downtimeReasonSettingPanelItem;
                    }
                }
                else
                {
                    // Plik z danymi nie istnieje, zwracamy domyślną listę
                    return await Task.FromResult(new List<DowntimeReasonSettingPanelItem>(new[]
                    {
                        new DowntimeReasonSettingPanelItem { Name = "S7.DowntimeReason", Value = "true", NameDevice = "iPC01", Status = "BRAK ZASILANIA", Location = "ODU_5", Source = "W16_iPC_Test"},
                    }));
                }
            }
        }
    }
}