using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsPublisher {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("dir")]
    public string Dir { get; set; }
}