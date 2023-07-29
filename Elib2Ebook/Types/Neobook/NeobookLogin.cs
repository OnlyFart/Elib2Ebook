using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookLogin {
    [JsonPropertyName("utoken")]
    public string Utoken { get; set; }
    
    [JsonPropertyName("uid")]
    public string Uid { get; set; }
}