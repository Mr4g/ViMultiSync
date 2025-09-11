using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public static class MachineStatusExtensions
    {
        public static Dictionary<string, object> ToMqttFormat(this MachineStatus status, bool stopsLine)
        {
            var value = status.StepOfStatus * 10 + (stopsLine ? 1 : 0);
            return new Dictionary<string, object>
            {
                {
                    "Alarms", new Dictionary<string, object>
                    {
                        {
                            CleanStatusName(status.Name),
                            new Dictionary<string, object>
                            {
                                { RemoveDiacritics(status.Status), value }
                            }
                        }
                    }
                }
            };
        }
        private static string CleanStatusName(string name)
        {
            // Usunięcie prefiksu "S1." oraz sufiksu "_IPC"
            return Regex.Replace(name, @"^S\d+\.", "").Replace("_IPC", "");
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // Zamień ręcznie "ł" na "l" i "Ł" na "L"
            result = result.Replace('ł', 'l').Replace('Ł', 'L');

            return result;
        }
    }
}
