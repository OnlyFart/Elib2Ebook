using System.Text.Json.Serialization;

namespace Core.Types.Neobook; 

public class NeobookTocChapter {
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
}