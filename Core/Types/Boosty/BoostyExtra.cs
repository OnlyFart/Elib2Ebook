using System.Text.Json.Serialization;

namespace Core.Types.Boosty;

public class BoostyExtra {
    [JsonPropertyName("isLast")]
    public bool IsLast { get; set; }
    
    [JsonPropertyName("offset")]
    public string Offset { get; set; }
}