using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexEpisodes
{
    [JsonPropertyName("episodes")]
    public BooksYandexEpisode[] Episodes { get; set; }
}
