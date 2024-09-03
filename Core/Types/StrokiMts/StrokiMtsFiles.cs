using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsFiles {
    [JsonPropertyName("preview")]
    public StrokiMtsFile Preview { get; set; }
    
    [JsonPropertyName("full")]
    public StrokiMtsFile[] Full { get; set; }
}