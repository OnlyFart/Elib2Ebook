using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexAudioBitrate
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
