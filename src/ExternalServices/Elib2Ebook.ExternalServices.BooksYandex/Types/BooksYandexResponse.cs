using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

public class BooksYandexResponse
{
    [JsonPropertyName("book")]
    public BooksYandexBook Book { get; set; }

    [JsonPropertyName("audiobook")]
    public BooksYandexBook AudioBook { get; set; }

    [JsonPropertyName("comicbook")]
    public BookmateComic Comicbook { get; set; }
}
