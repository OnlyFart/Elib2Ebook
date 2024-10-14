using System.Text.Json.Serialization;

namespace Core.Types.FanFicus;

public class FanFicusBook {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("creatorId")]
    public FanFicusCreator[] Creators { get; set; }
    
    [JsonPropertyName("images")]
    public FanFicusImage[] Images { get; set; }
}