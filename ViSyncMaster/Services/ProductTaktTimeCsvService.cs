using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ViSyncMaster.Services
{
    public sealed class ProductTaktTimeCsvService
    {
        private readonly string _filePath;
        private readonly Dictionary<string, double> _cache = new(StringComparer.OrdinalIgnoreCase);

        public ProductTaktTimeCsvService(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        public bool TryGetTaktSeconds(string productName, out double taktSeconds)
        {
            return _cache.TryGetValue(productName?.Trim() ?? string.Empty, out taktSeconds);
        }

        public void UpsertTaktSeconds(string productName, double taktSeconds)
        {
            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("productName is required");
            if (taktSeconds <= 0)
                throw new ArgumentException("taktSeconds must be > 0");

            _cache[productName.Trim()] = taktSeconds;
            Save();
        }

        private void Load()
        {
            _cache.Clear();
            if (!File.Exists(_filePath))
                return;

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant()
            };

            using var sr = new StreamReader(_filePath);
            using var csv = new CsvReader(sr, cfg);
            var records = csv.GetRecords<ProductTaktRow>().ToList();
            foreach (var r in records)
            {
                if (!string.IsNullOrWhiteSpace(r.productName) && r.median_takt_s > 0)
                    _cache[r.productName.Trim()] = r.median_takt_s;
            }
        }

        private void Save()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture);
            using var sw = new StreamWriter(_filePath, false);
            using var csv = new CsvWriter(sw, cfg);
            csv.WriteRecords(_cache.OrderBy(x => x.Key).Select(x => new ProductTaktRow
            {
                productName = x.Key,
                median_takt_s = x.Value
            }));
        }

        private sealed class ProductTaktRow
        {
            public string productName { get; set; } = string.Empty;
            public double median_takt_s { get; set; }
        }
    }
}
