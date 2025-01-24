using System;
using System.Text.Json;

namespace ViSyncMaster.DeepCopy
{
    public static class DeepCopyExtensions
    {
        public static T DeepCopy<T>(this T self)
        {
            var serialized = JsonSerializer.Serialize(self);
            return JsonSerializer.Deserialize<T>(serialized);
        }
    }
}
