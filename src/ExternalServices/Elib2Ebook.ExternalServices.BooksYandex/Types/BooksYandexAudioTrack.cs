using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BooksYandex.Types;

internal class BooksYandexAudioTrack
{
    [JsonPropertyName("offline")]
    public BooksYandexAudioOffline Offline { get; set; }
}
