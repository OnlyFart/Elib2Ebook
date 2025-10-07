using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsContentInfo {
    [JsonPropertyName("fileType")]
    public string FileType { get; set; }
}