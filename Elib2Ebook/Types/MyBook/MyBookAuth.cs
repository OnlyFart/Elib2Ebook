using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.MyBook; 

public class MyBookAuth {
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("secret")]
    public string Secret { get; set; }
}