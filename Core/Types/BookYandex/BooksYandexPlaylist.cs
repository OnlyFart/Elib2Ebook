using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexPlaylist {
    [JsonPropertyName("tracks")]
    public BooksYandexAudioTrack[] Tracks { get; set; }
}