using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexComicMetadata {
    [JsonPropertyName("pages")]
    public BooksYandexComicPage[] Pages { get; set; }
}