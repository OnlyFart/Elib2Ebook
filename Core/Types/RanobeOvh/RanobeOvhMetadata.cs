using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanobeOvhMetadata {
    [JsonPropertyName("type")]
    public string Type { get; set; }
}