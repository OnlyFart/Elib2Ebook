using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexUri
{
    [JsonPropertyName("Image")]
    public string Image { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }
}
