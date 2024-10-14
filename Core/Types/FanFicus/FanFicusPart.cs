using System.Text.Json.Serialization;

namespace Core.Types.FanFicus;

public class FanFicusPart {
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
}