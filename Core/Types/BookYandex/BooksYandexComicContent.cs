using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexComicContent {
    [JsonPropertyName("uris")]
    public BooksYandexUri Uri { get; set; }
}