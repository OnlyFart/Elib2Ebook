using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexCover {
    [JsonPropertyName("large")]
    public string Large { get; set; }
    
    [JsonPropertyName("small")]
    public string Small { get; set; }
}