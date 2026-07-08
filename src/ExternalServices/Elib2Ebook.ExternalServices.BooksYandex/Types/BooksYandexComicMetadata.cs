using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexComicMetadata
{
    [JsonPropertyName("pages")]
    public BooksYandexComicPage[] Pages { get; set; }
}
