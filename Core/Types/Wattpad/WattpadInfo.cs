using System.Text.Json.Serialization;

namespace Core.Types.Wattpad; 

public class WattpadInfo {
    [JsonPropertyName("user")]
    public WattpadUser User { get; set; }
    
    [JsonPropertyName("cover")]
    public string Cover { get; set; }
    
    [JsonPropertyName("parts")]
    public WattpadPart[] Parts { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
}