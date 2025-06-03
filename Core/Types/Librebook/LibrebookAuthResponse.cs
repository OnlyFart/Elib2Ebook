using System.Text.Json.Serialization;

namespace Core.Types.Librebook;

public class LibrebookAuthResponse {
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
}