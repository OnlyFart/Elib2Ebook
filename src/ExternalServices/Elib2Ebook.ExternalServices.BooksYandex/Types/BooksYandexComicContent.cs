using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexComicContent
{
    [JsonPropertyName("uris")]
    public BooksYandexUri Uri { get; set; }
}
