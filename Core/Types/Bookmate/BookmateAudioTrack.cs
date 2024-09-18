using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateAudioTrack {
    [JsonPropertyName("offline")]
    public BookmateAudioOffline Offline { get; set; }
}