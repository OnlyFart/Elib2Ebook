using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response; 

public class LitresPerson<TId> {
    [JsonPropertyName("id")]
    public TId Id { get; set; }
    
    [JsonPropertyName("full_name")]
    public string FullName { get; set; }
    
    [JsonPropertyName("role")]
    public string Role { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; }
}