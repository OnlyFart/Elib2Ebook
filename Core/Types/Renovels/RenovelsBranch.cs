using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsBranch {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("publishers")]
    public RenovelsPublisher[] Publishers { get; set; }
}