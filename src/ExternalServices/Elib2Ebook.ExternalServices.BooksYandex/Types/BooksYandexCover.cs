using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

public class BooksYandexCover
{
    [JsonPropertyName("large")]
    public string Large { get; set; }

    [JsonPropertyName("small")]
    public string Small { get; set; }
}
