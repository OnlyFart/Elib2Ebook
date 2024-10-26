using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexEpisodes {
    [JsonPropertyName("episodes")]
    public BooksYandexEpisode[] Episodes { get; set; }
}