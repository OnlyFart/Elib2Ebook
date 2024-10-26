using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexAudioBitrate {
    [JsonPropertyName("url")]
    public string Url { get; set; }
}