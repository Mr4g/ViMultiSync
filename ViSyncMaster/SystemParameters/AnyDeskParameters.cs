using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ViSyncMaster.SystemParameters
{
    public class AnyDeskParameters
    {
        /// <summary>
        /// The extracted AnyDesk ID (or "Not found").
        /// </summary>
        public string? AnyDeskId { get; private set; }

        /// <summary>
        /// Fetches the AnyDesk client ID by reading service.conf under %ProgramData%\AnyDesk.
        /// </summary>
        /// <returns>
        /// The numeric AnyDesk ID, or "Not found" if missing.
        /// </returns>
        public string FetchAnyDeskId()
        {
            // Tylko katalog ProgramData
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string baseDir = Path.Combine(programData, "AnyDesk");

            if (!Directory.Exists(baseDir))
            {
                AnyDeskId = "Not found";
                return AnyDeskId;
            }

            // Znajdź wszystkie pliki service.conf w podkatalogach
            var confFiles = Directory.GetFiles(baseDir, "system.conf", SearchOption.AllDirectories);

            // Regex wyszukujący zarówno ad.anynet.id, ad.id, jak i id
            var regex = new Regex(@"\b(?:ad\.anynet\.id|ad\.id|id)\s*=\s*(\d{6,12})", RegexOptions.Compiled);

            foreach (var file in confFiles)
            {
                try
                {
                    foreach (var line in File.ReadAllLines(file))
                    {
                        var m = regex.Match(line);
                        if (m.Success)
                        {
                            AnyDeskId = m.Groups[1].Value;
                            return AnyDeskId;
                        }
                    }
                }
                catch
                {
                    // Jeśli odczyt pliku się nie powiedzie, próbujemy kolejny
                }
            }

            AnyDeskId = "Not found";
            return AnyDeskId;
        }
    }
}
