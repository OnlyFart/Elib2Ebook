using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Response; 

public class LitresPerson<TId> {
    [JsonPropertyName("id")]
    public TId Id { get; set; }
    
    [JsonPropertyName("full_name")]
    public string FullName { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; }
}