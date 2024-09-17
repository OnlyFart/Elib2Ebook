using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response;

public class LitresFile {
    [JsonPropertyName("extension")]
    public string Extension { get; set; }
    
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("mime")]
    public string Mime { get; set; }
}