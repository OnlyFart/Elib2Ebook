using System.Text.Json.Serialization;

namespace Core.Types.Litnet; 

public class LitnetAuthResponse {
    [JsonPropertyName("error")]
    public string Error { get; set; }
    
    [JsonPropertyName("token")]
    public string Token { get; set; }
}