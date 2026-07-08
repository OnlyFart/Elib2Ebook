using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexComicPage
{
    [JsonPropertyName("content")]
    public BooksYandexComicContent Content { get; set; }
}
