using System.Text.Json.Serialization;

namespace Core.Types.Freedlit; 

public class FreedlitAuthError {
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("password")]
    public string[] Password { get; set; }
}