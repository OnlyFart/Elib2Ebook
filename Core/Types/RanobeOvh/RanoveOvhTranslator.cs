using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanoveOvhTranslator {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}