using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookLogin {
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("uid")]
    public string Uid { get; set; }
}