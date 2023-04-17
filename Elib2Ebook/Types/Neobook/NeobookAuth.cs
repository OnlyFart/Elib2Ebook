using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookAuth {
    [JsonPropertyName("error")]
    public string Error { get; set; }
    
    [JsonPropertyName("login")]
    public NeobookLogin Login { get; set; }
}