using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexPlaylist
{
    [JsonPropertyName("tracks")]
    public BooksYandexAudioTrack[] Tracks { get; set; }
}
