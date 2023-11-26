using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using ViMultiSync.DataModel;
using System.Linq;

namespace ViMultiSync.Services
{
    public class ConfigService
    {
        private const string configFileName = "ConfigData.json";

        public async Task<AppConfigData> GetAppConfigDataAsync()
        {
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", configFileName);


            if (File.Exists(configFilePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(configFilePath);
                    var configData = JsonSerializer.Deserialize<List<AppConfigData>>(jsonData) ?? new List<AppConfigData>();

                    // Zwróć pierwszy obiekt lub null, jeśli lista jest pusta
                    return configData.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd odczytu pliku konfiguracyjnego: {ex.Message}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"Plik konfiguracyjny nie istnieje: {configFilePath}");
                return null;
            }
        }
    }
}