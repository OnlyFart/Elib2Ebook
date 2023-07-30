using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsAuthResponse {
    [JsonPropertyName("access_token")]
    public string Token { get; set; }
}