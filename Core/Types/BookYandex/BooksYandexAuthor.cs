using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexAuthor {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
}