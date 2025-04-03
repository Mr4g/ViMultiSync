using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel.Extension
{
    public static class ExtensionTestingMessage
    {
        public static Dictionary<string, object> ToMqttFormatForTestingMessage(this MachineStatus status)
        {
            return new Dictionary<string, object>
            {
                { "Pieces", new Dictionary<string, object>
                    {
                        { CleanStatusName(status.Name), status.Value }
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
