using System.Text.Json.Serialization;

namespace Core.Types.Litres.Requests; 

public class LitresBrowseArtsData {
    [JsonPropertyName("currency")]
    public string Currency { get; set; }
    
    [JsonPropertyName("anno")]
    public string Anno { get; set; }
    
    [JsonPropertyName("id")]
    public string[] Id { get; set; }
}