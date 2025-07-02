using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Heleprs
{
    public static class StatusParser
    {
        public static string GetPanelKey(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return string.Empty;

            var words = status.ToUpperInvariant().Split(new[] { ' ', '&' }, StringSplitOptions.RemoveEmptyEntries);

            // Jeśli jest więcej niż jedno słowo, próbujemy użyć drugiego
            if (words.Length >= 2)
                return words[1];

            // W przeciwnym razie używamy pierwszego
            return words[0];
        }
    }
}
