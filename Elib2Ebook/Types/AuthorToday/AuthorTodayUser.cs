using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.AuthorToday; 

public class AuthorTodayUser {
    [JsonPropertyName("id")]
    public long Id { get; set; }
}