using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ViSyncMaster.Services
{
    public sealed class ProductTaktTimeCsvService
    {
        private readonly string _filePath;
        private readonly Dictionary<string, double> _cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex ProductNumberRegex = new(@"(?<!\d)\d{7}(?!\d)", RegexOptions.Compiled);

        public ProductTaktTimeCsvService(string filePath)
        {
            _filePath = filePath;
            try
            {
                Load();
            }
            catch
            {
                _cache.Clear();
            }
        }

        public bool TryGetTaktSeconds(string productName, out double taktSeconds)
        {
            var key = NormalizeProductKey(productName);
            var found = _cache.TryGetValue(key, out taktSeconds);
            if (!found)
            {
                var sampleKeys = string.Join(", ", _cache.Keys.Take(5));
                Debug.WriteLine($"[ProductTaktTimeCsvService] CSV lookup MISS | path={_filePath} | key='{key}' | sampleKeys=[{sampleKeys}]");
            }
            return found;
        }

        public void UpsertTaktSeconds(string productName, double taktSeconds)
        {
            var key = NormalizeProductKey(productName);
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("productName is required");
            if (taktSeconds <= 0)
                throw new ArgumentException("taktSeconds must be > 0");

            _cache[key] = taktSeconds;
            try
            {
                Save();
            }
            catch
            {
                // Nie blokuj działania tabeli Result gdy zapis CSV chwilowo się nie powiedzie.
            }
        }

        private void Load()
        {
            _cache.Clear();
            var exists = File.Exists(_filePath);
            Debug.WriteLine($"[ProductTaktTimeCsvService] Load | path={_filePath} | exists={exists}");
            if (!exists)
                return;

            var lines = File.ReadAllLines(_filePath, Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (!lines.Any())
            {
                Debug.WriteLine($"[ProductTaktTimeCsvService] Load | path={_filePath} | empty file");
                return;
            }

            var delimiter = DetectDelimiter(lines[0]);
            var header = ParseCsvLine(lines[0], delimiter)
                .Select(NormalizeHeader)
                .ToList();

            var productIndex = header.FindIndex(h => h == "productname");
            var taktIndex = header.FindIndex(h => h == "median_takt_s");

            if (productIndex < 0 || taktIndex < 0)
            {
                Debug.WriteLine($"[ProductTaktTimeCsvService] Load | path={_filePath} | invalid header");
                return;
            }

            for (int i = 1; i < lines.Count; i++)
            {
                var cols = ParseCsvLine(lines[i], delimiter);
                if (cols.Count <= Math.Max(productIndex, taktIndex))
                    continue;

                var key = NormalizeProductKey(cols[productIndex]);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (TryParsePositiveDouble(cols[taktIndex], out var takt))
                    _cache[key] = takt;
            }

            Debug.WriteLine($"[ProductTaktTimeCsvService] Load | path={_filePath} | records={_cache.Count}");
        }

        private void Save()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ","
            };
            using var sw = new StreamWriter(_filePath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            using var csv = new CsvWriter(sw, cfg);
            csv.WriteHeader<ProductTaktRow>();
            csv.NextRecord();
            foreach (var entry in _cache.OrderBy(x => x.Key))
            {
                csv.WriteRecord(new ProductTaktRow
                {
                    productName = entry.Key,
                    median_takt_s = entry.Value
                });
                csv.NextRecord();
            }
            Debug.WriteLine($"[ProductTaktTimeCsvService] Save | path={_filePath} | records={_cache.Count}");
        }

        private static string NormalizeHeader(string header)
        {
            return RemoveNoise(header).Trim().ToLowerInvariant();
        }

        private static string NormalizeProductKey(string value)
        {
            var clean = RemoveNoise(value).Replace("\"", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clean))
                return string.Empty;

            var matches = ProductNumberRegex.Matches(clean);
            if (matches.Count > 0)
                return matches[^1].Value;

            return clean;
        }

        private static string RemoveNoise(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var withoutBom = value.Replace("\uFEFF", string.Empty);
            var filtered = new string(withoutBom.Where(c => !char.IsControl(c) || c == '\t').ToArray());
            return filtered.Trim();
        }

        private static char DetectDelimiter(string headerLine)
        {
            var semicolons = headerLine.Count(c => c == ';');
            var commas = headerLine.Count(c => c == ',');
            return semicolons > commas ? ';' : ',';
        }

        private static List<string> ParseCsvLine(string line, char delimiter)
        {
            var result = new List<string>();
            if (line == null)
                return result;

            var sb = new StringBuilder();
            var inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (ch == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(ch);
            }
            result.Add(sb.ToString());
            return result;
        }

        private static bool TryParsePositiveDouble(string raw, out double value)
        {
            value = 0;
            var clean = RemoveNoise(raw).Replace("\"", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clean))
                return false;

            if (double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0)
                return true;
            if (double.TryParse(clean, NumberStyles.Float, CultureInfo.GetCultureInfo("pl-PL"), out value) && value > 0)
                return true;

            var normalized = clean.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0)
                return true;

            value = 0;
            return false;
        }

        private sealed class ProductTaktRow
        {
            public string productName { get; set; } = string.Empty;
            public double median_takt_s { get; set; }
        }
    }
}
