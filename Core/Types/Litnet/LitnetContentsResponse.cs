using System.Text.Json.Serialization;

namespace Core.Types.Litnet; 

public class LitnetContentsResponse {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
}