using System;
using System.Collections.Generic;
using System.Linq;

namespace ViSyncMaster.DataModel
{
    public class MessagePgToSplunk
    {
        private readonly Dictionary<string, string> _states;

        public string Producing
        {
            get => _states[nameof(Producing)];
            set => SetExclusiveValue(nameof(Producing), value);
        }

        public string Waiting
        {
            get => _states[nameof(Waiting)];
            set => SetExclusiveValue(nameof(Waiting), value);
        }

        public string MaintenanceMode
        {
            get => _states[nameof(MaintenanceMode)];
            set => SetExclusiveValue(nameof(MaintenanceMode), value);
        }

        public string SettingMode
        {
            get => _states[nameof(SettingMode)];
            set => SetExclusiveValue(nameof(SettingMode), value);
        }

        public string MachineDowntime
        {
            get => _states[nameof(MachineDowntime)];
            set => SetExclusiveValue(nameof(MachineDowntime), value);
        }
        public string LogisticMode
        {
            get => _states[nameof(LogisticMode)];
            set => SetExclusiveValue(nameof(LogisticMode), value);
        }

        public string ProductionMode
        {
            get => _states[nameof(ProductionMode)];
            set => SetExclusiveValue(nameof(ProductionMode), value);
        }

        // Konstruktor domyślny
        public MessagePgToSplunk()
        {
            _states = new Dictionary<string, string>
            {
                { nameof(Producing), "false" },
                { nameof(Waiting), "true" }, // Domyślna wartość
                { nameof(MaintenanceMode), "false" },
                { nameof(SettingMode), "false" },
                { nameof(MachineDowntime), "false" },
                { nameof(LogisticMode), "false" },
                { nameof(ProductionMode), "false" }
            };
        }

        /// <summary>
        /// Ustawia jedną właściwość na "true", a pozostałe na "false".
        /// </summary>
        /// <param name="propertyName">Nazwa właściwości, którą chcemy ustawić na "true".</param>
        /// <param name="value">Nowa wartość dla danej właściwości.</param>
        private void SetExclusiveValue(string propertyName, string value)
        {
            if (value != "true" && value != "false")
                throw new ArgumentException("Wartość musi być 'true' lub 'false'.");

            if (value == "true")
            {
                ResetValues();
                _states[propertyName] = "true";
            }
        }

        /// <summary>
        /// Resetuje wszystkie właściwości na "false".
        /// </summary>
        private void ResetValues()
        {
            foreach (var key in _states.Keys)
            {
                _states[key] = "false";
            }
        }

        /// <summary>
        /// Ustawia wartość na podstawie licznika.
        /// </summary>
        /// <param name="counter">Licznik określający, która właściwość ma być ustawiona na "true".</param>
        public void SetByCounter(int counter)
        {
            if (counter < 1 || counter > 7)
                throw new ArgumentOutOfRangeException(nameof(counter), "Licznik musi być w zakresie od 1 do 5.");

            ResetValues();

            var stateName = ((State)counter).ToString();
            _states[stateName] = "true";
        }

        public override string ToString()
        {
            return string.Join(", ", _states.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        // Typ wyliczeniowy dla lepszej czytelności
        private enum State
        {
            MachineDowntime = 1,
            LogisticMode = 2,
            SettingMode = 3, 
            MaintenanceMode = 4,
            ProductionMode = 5,
            Producing = 6,
            Waiting = 7
        }
    }
}
