using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexAudioTrack {
    [JsonPropertyName("offline")]
    public BooksYandexAudioOffline Offline { get; set; }
}