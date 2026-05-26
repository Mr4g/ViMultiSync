using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ViSyncMaster.Services
{
    public class FirstPartFieldConfigService
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly object _lock = new();

        public FirstPartFieldConfigService(string filePath)
        {
            _filePath = filePath;
            EnsureFileExists();
        }

        public List<string>? GetVisibleFieldsForProduct(string productNumber)
        {
            var all = LoadAll();
            return all.TryGetValue(productNumber, out var fields) ? fields : null;
        }

        public void SaveVisibleFieldsForProduct(string productNumber, List<string> fields)
        {
            lock (_lock)
            {
                var all = LoadAll();
                all[productNumber] = fields.Distinct().ToList();

                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var tempPath = _filePath + ".tmp";
                var json = JsonSerializer.Serialize(all, _jsonOptions);
                File.WriteAllText(tempPath, json);

                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                File.Move(tempPath, _filePath);
            }
        }

        private Dictionary<string, List<string>> LoadAll()
        {
            lock (_lock)
            {
                EnsureFileExists();
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                return data ?? new Dictionary<string, List<string>>();
            }
        }

        private void EnsureFileExists()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "{}");
            }
        }
    }
}
