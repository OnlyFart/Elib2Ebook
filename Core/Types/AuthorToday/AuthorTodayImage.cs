using System.Text.Json.Serialization;

namespace Core.Types.AuthorToday;

public class AuthorTodayImage {
    [JsonPropertyName("url")]
    public string Url { get; set; }
}