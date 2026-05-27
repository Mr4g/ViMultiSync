using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class InstructionService
    {
        private readonly string _instructionsRootPath;
        private readonly string _manifestPath;

        public InstructionService(string instructionsRootPath)
        {
            _instructionsRootPath = instructionsRootPath;
            _manifestPath = Path.Combine(_instructionsRootPath, "manifest.json");
        }

        public bool TryGetInstructionForProduct(string productNumber, out string instructionUrl, out string instructionTitle, out string errorMessage)
        {
            instructionUrl = string.Empty;
            instructionTitle = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(productNumber))
            {
                errorMessage = "Brak aktualnego numeru produktu.";
                return false;
            }

            if (!File.Exists(_manifestPath))
            {
                errorMessage = $"Brak pliku manifestu: {_manifestPath}";
                return false;
            }

            try
            {
                var json = File.ReadAllText(_manifestPath);
                var manifest = JsonSerializer.Deserialize<InstructionManifest>(json);

                if (manifest?.Instructions == null || manifest.Instructions.Count == 0)
                {
                    errorMessage = "Manifest instrukcji jest pusty.";
                    return false;
                }

                var item = manifest.Instructions.FirstOrDefault(x => x.ProductNumber == productNumber);
                if (item == null)
                {
                    errorMessage = $"Brak instrukcji dla produktu: {productNumber}";
                    return false;
                }

                var fullPath = Path.Combine(_instructionsRootPath, item.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                    errorMessage = $"Brak pliku instrukcji: {fullPath}";
                    return false;
                }

                instructionUrl = new Uri(fullPath).AbsoluteUri;
                instructionTitle = string.IsNullOrWhiteSpace(item.Title)
                    ? $"Instrukcja {productNumber}"
                    : item.Title;

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd podczas odczytu instrukcji dla produktu {ProductNumber}", productNumber);
                errorMessage = "Nie udało się wczytać instrukcji. Sprawdź manifest i plik PDF.";
                return false;
            }
        }
    }
}
