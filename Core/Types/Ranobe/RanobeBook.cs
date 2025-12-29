using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.Ranobe; 

public class RanobeBook {
    [JsonPropertyName("title")]
    public string Title { get; set; }
        
    [JsonPropertyName("author")]
    public string Author { get; set; }
        
    [JsonPropertyName("verticalImages")]
    public RanobeImage[] Images { get; set; }
    
    [JsonPropertyName("verticalImage")]
    public RanobeImage Image { get; set; }
        
    [JsonPropertyName("chapters")]
    public List<RanobeChapterShort> Chapters { get; set; }
}