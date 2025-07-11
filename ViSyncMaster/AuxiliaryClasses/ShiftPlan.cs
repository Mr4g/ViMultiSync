// using directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ViSyncMaster.AuxiliaryClasses
{
    /// <summary>
    /// Reprezentuje pojedynczy plan zmiany oraz zarządza wczytywaniem planów z pliku JSON.
    /// </summary>
    public record ShiftPlan
    {
        /// <summary>Domyślna ścieżka do pliku z konfiguracją planów zmian.</summary>
        public const string DefaultConfigPath = @"C:\ViSM\ConfigFiles\shiftPlans.json";

        /// <summary>Nazwa działu, z którego pochodzi ten plan.</summary>
        [JsonIgnore]
        public string Department { get; init; }

        /// <summary>Numer zmiany (1, 2 lub 3).</summary>
        public int ShiftNumber { get; init; }
        public TimeSpan ShiftStart { get; init; }
        public TimeSpan ShiftEnd { get; init; }
        public TimeSpan PlanStart { get; init; }
        public TimeSpan ShutDown { get; init; }

        /// <summary>Lista przerw w ramach zmiany.</summary>
        public List<ShiftBreak> Breaks { get; init; } = new();

        // Pomocniczy model do deserializacji JSON
        private class DepartmentPlan
        {
            public string Name { get; set; }
            public List<ShiftPlan> Shifts { get; set; }
        }

        private class Root
        {
            public List<DepartmentPlan> Departments { get; set; }
        }

        // Wewnętrzna kolekcja wszystkich planów
        private static List<ShiftPlan> _allPlans;

        /// <summary>
        /// Wczytuje plik JSON z planami zmian spod domyślnej ścieżki.
        /// </summary>
        public static void LoadFromJson()
        {
            LoadFromJson(DefaultConfigPath);
        }

        /// <summary>
        /// Wczytuje plik JSON z planami zmian podanej ścieżki.
        /// </summary>
        /// <param name="jsonFilePath">Pełna ścieżka do pliku JSON.</param>
        public static void LoadFromJson(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException($"Nie znaleziono pliku z planami zmian: {jsonFilePath}");

            var json = File.ReadAllText(jsonFilePath);
            var root = JsonConvert.DeserializeObject<Root>(json)
                       ?? throw new InvalidDataException("Nieprawidłowa struktura pliku JSON.");

            _allPlans = root.Departments
                .SelectMany(d => d.Shifts.Select(s => s with { Department = d.Name }))
                .ToList();
        }

        /// <summary>
        /// Zwraca aktywny plan zmiany dla określonego działu na podstawie aktualnego czasu.
        /// Jeśli czas przypada na nocną zmianę, uwzględnia "przeskok" przez północ.
        /// </summary>
        /// <param name="department">Nazwa działu (np. "KM", "SK").</param>
        /// <param name="now">Opcjonalnie: czas do sprawdzenia; domyślnie DateTime.Now.</param>
        /// <returns>Obiekt ShiftPlan dla bieżącej zmiany.</returns>
        public static ShiftPlan GetCurrent(string department, DateTime? now = null)
        {
            if (_allPlans == null)
                throw new InvalidOperationException(
                    "Plany zmian nie zostały wczytane. Wywołaj ShiftPlan.LoadFromJson().");

            var deptPlans = _allPlans
                .Where(s => string.Equals(s.Department, department, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!deptPlans.Any())
                throw new ArgumentException($"Brak planów dla działu '{department}'.");

            var currentTime = (now ?? DateTime.Now).TimeOfDay;

            // 1. Poszukiwanie zmian dziennych (bez przewijania przez północ)
            var dayShift = deptPlans
                .FirstOrDefault(s => s.ShiftStart <= s.ShiftEnd
                                     && currentTime >= s.ShiftStart
                                     && currentTime < s.ShiftEnd);
            if (dayShift != null)
                return dayShift;

            // 2. Poszukiwanie zmiany nocnej (ShiftStart > ShiftEnd)
            var nightShift = deptPlans
                .FirstOrDefault(s => s.ShiftStart > s.ShiftEnd
                                     && (currentTime >= s.ShiftStart || currentTime < s.ShiftEnd));
            if (nightShift != null)
                return nightShift;

            throw new InvalidOperationException(
                "Nie udało się dopasować żadnej zmiany do bieżącego czasu.");
        }
    }

    /// <summary>
    /// Model reprezentujący przerwę w ramach zmiany.
    /// </summary>
    public record ShiftBreak(TimeSpan Start, TimeSpan End);
}