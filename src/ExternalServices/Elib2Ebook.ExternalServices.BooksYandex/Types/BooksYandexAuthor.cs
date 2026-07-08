using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

public class BooksYandexAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
}
