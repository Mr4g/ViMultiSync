using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using System.Linq;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using ViSyncMaster.AuxiliaryClasses;

namespace ViSyncMaster.Services
{
    public class ConfigService
    {
        private string Hostname = Environment.MachineName;

        public async Task<AppConfigData> GetAppConfigDataAsync()
        {
        string configFilePath = Path.Combine("C:", "ViSM", "ConfigFiles", $"{Hostname}.json");

            if (File.Exists(configFilePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(configFilePath);
                    var jsonDoc = JsonDocument.Parse(jsonData);

                    // Pobierz tylko ConfigData (zakładając, że JSON ma tę strukturę)
                    var configData = jsonDoc.RootElement
                                            .GetProperty("ConfigData")
                                            .EnumerateArray()
                                            .Select(item => JsonSerializer.Deserialize<AppConfigData>(item.GetRawText()))
                                            .ToList();

                    // Zwróć pierwszy obiekt z ConfigData lub null, jeśli lista jest pusta
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
                await ErrorHandler.ShowMissingFileError($"Brak pliku konfiguracyjnego: {configFilePath}");
                return null;
            }
        }
    }
}