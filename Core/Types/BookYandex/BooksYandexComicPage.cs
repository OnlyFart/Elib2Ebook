using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexComicPage {
    [JsonPropertyName("content")]
    public BooksYandexComicContent Content { get; set; }
}