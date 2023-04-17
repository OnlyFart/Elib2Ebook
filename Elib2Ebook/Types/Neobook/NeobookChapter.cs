using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookChapter {
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("access")]
    public string Access { get; set; }
}