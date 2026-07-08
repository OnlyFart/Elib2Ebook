using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexAudioOffline
{
    [JsonPropertyName("max_bit_rate")]
    public BooksYandexAudioBitrate Max { get; set; }

    [JsonPropertyName("min_bit_rate")]
    public BooksYandexAudioBitrate Min { get; set; }
}
