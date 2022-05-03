using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litmarket; 

public class AuthResponse {
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}