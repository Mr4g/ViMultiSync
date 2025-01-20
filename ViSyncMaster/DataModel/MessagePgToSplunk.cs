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

        public string Maintenance
        {
            get => _states[nameof(Maintenance)];
            set => SetExclusiveValue(nameof(Maintenance), value);
        }

        public string Setting
        {
            get => _states[nameof(Setting)];
            set => SetExclusiveValue(nameof(Setting), value);
        }

        public string Downtime
        {
            get => _states[nameof(Downtime)];
            set => SetExclusiveValue(nameof(Downtime), value);
        }

        // Konstruktor domyślny
        public MessagePgToSplunk()
        {
            _states = new Dictionary<string, string>
            {
                { nameof(Producing), "false" },
                { nameof(Waiting), "true" }, // Domyślna wartość
                { nameof(Maintenance), "false" },
                { nameof(Setting), "false" },
                { nameof(Downtime), "false" }
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
            if (counter < 1 || counter > 5)
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
            Producing = 1,
            Waiting = 2,
            Maintenance = 3,
            Setting = 4,
            Downtime = 5
        }
    }
}
