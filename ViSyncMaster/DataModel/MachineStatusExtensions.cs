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
        public static Dictionary<string, object> ToMqttFormat(this MachineStatus status)
        {
            return new Dictionary<string, object>
            {
                {
                    "Alarms", new Dictionary<string, object>
                    {
                        {
                            CleanStatusName(status.Name),
                            new Dictionary<string, object>
                            {
                                { RemoveDiacritics(status.Status), status.StepOfStatus }
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

            return string.Concat(text
                .Normalize(NormalizationForm.FormD) // Zamienia znaki na formę rozłożoną (np. "ą" → "a" + diakrytyk)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)); // Usuwa diakrytyki
        }
    }
}
