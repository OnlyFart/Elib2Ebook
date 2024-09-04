using System.Text.Json.Serialization;

namespace Core.Types.AuthorToday; 

public class AuthorTodayAuthResponse {
    [JsonPropertyName("message")]
    public string Message { get; set; }
    
    [JsonPropertyName("token")]
    public string Token { get; set; }
}