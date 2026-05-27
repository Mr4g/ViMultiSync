using System.Text.Json.Serialization;

namespace ViSyncMaster.DataModel
{
    public class InstructionItem
    {
        [JsonPropertyName("productNumber")]
        public string ProductNumber { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;
        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = string.Empty;
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }
}
