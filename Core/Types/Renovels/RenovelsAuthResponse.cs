using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsAuthResponse {
    [JsonPropertyName("access_token")]
    public string Token { get; set; }
}