using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Requests; 

public class LitresAuthData {
    [JsonPropertyName("login")]
    public string Login { get; set; }
    
    [JsonPropertyName("pwd")]
    public string Password { get; set; }
}