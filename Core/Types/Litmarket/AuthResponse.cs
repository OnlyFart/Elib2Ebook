using System.Text.Json.Serialization;

namespace Core.Types.Litmarket; 

public class AuthResponse {
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}