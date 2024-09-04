using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsFile {
    [JsonPropertyName("fileId")]
    public long FileId { get; set; }
}