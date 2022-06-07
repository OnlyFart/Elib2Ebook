using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.AuthorToday; 

public class AuthorTodayAuthResponse {
    [JsonPropertyName("message")]
    public string Message { get; set; }
    
    [JsonPropertyName("token")]
    public string Token { get; set; }
}