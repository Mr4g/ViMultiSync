using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ViSyncMaster.DataModel
{
    public class InstructionManifest
    {
        [JsonPropertyName("instructions")]
        public List<InstructionItem> Instructions { get; set; } = new();
    }
}
