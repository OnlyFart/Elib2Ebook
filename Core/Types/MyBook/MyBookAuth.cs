using System.Text.Json.Serialization;

namespace Core.Types.MyBook; 

public class MyBookAuth {
    [JsonPropertyName("token")]
    public string Token { get; set; }
    
    [JsonPropertyName("secret")]
    public string Secret { get; set; }
}