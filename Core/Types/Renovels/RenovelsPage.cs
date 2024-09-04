using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsPage {
    [JsonPropertyName("link")]
    public string Link { get; set; }
}