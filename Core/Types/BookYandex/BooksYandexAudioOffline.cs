using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexAudioOffline {
    [JsonPropertyName("max_bit_rate")]
    public BooksYandexAudioBitrate Max { get; set; }
    
    [JsonPropertyName("min_bit_rate")]
    public BooksYandexAudioBitrate Min { get; set; }
}