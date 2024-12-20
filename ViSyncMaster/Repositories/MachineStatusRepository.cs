using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using ViSyncMaster.DataModel;
using System.IO;

namespace ViSyncMaster.Repositories
{
    public class MachineStatusRepository
    {
        private readonly string _filePath = "statuses.json";

        // Wczytanie statusów z pliku
        public List<MachineStatus> LoadStatuses(bool onlyActiveStatuses)
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<MachineStatus>();

                var json = File.ReadAllText(_filePath);
                var allStatuses = JsonSerializer.Deserialize<List<MachineStatus>>(json) ?? new List<MachineStatus>();

                // Zwracamy tylko aktywne statusy, jeśli ustawiono `onlyActiveStatuses`
                if (onlyActiveStatuses)
                {
                    return allStatuses.Where(status => status.IsActive).ToList();
                }
                // Jeśli `onlyActiveStatuses` jest `false`, zwracamy wszystkie statusy
                return allStatuses;
            }
            catch
            {
                // Obsługuje błąd, np. brak pliku
                return new List<MachineStatus>();
            }
        }

        // Zapisanie pojedynczego statusu
        public void SaveStatus(MachineStatus status)
        {
            var statuses = LoadStatuses(false);
            statuses.Add(status);
            SaveStatuses(statuses);
        }

        // Zapisanie statusów do pliku
        public void SaveStatuses(IEnumerable<MachineStatus> statuses)
        {
            {
                // Konwertuje statusy aby je edytować
                var statusList = statuses.ToList();

                // Jeśli liczba statusów przekracza 100, usuwa najstarszy

                    // Sortowanie według ID (czas dodania w Unix Time Epoch)
                    statusList = statusList
                        .OrderByDescending(status => status.Id) // Najnowsze statusy najpierw
                        .Take(100)                              // Zachowaj tylko 100 najnowszych
                        .ToList();                              // Konwertuj z powrotem na listę
            

                // Serializuj i zapisz do pliku
                var json = JsonSerializer.Serialize(statusList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        public void UpdateStatus(MachineStatus updatedStatus)
        {
            var statuses = LoadStatuses(false);

            // Szukamy statusu o tym samym ID
            var existingStatus = statuses.FirstOrDefault(status => status.Id == updatedStatus.Id);

            if (existingStatus != null)
            {
                // Aktualizujemy status
                existingStatus.Status = updatedStatus.Status;
                existingStatus.Reason = updatedStatus.Reason;
                existingStatus.EndTime = updatedStatus.EndTime;  
            }

            // Zapisujemy zaktualizowane statusy
            SaveStatuses(statuses);
        }

    }
}
