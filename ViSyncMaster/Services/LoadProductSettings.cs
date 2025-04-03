using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public static class LoadProductSettings
    {

        public static List<ProductSettings> LoadSettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Plik {filePath} nie istnieje.");
                return new List<ProductSettings>(); // Zwróć pustą listę, jeśli plik nie istnieje
            }

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower()  // Ignoruje wielkość liter
                };

                using (var reader = new StreamReader(filePath))  // Używamy poprawnej ścieżki
                using (var csv = new CsvReader(reader, config))
                {
                    return csv.GetRecords<ProductSettings>().ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas wczytywania CSV: {ex.Message}");
                return new List<ProductSettings>(); // Zwróć pustą listę w razie błędu
            }
        }
    }
}
