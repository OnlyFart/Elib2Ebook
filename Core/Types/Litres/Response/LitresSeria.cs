using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response; 

public class LitresSeria {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("sequence_number")]
    public string SequenceNumber { get; set; }
}