using System.Text.Json.Serialization;

namespace Core.Types.AuthorToday; 

public class AuthorTodayUser {
    [JsonPropertyName("id")]
    public long Id { get; set; }
}