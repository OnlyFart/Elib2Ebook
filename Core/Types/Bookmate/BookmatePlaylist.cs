using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmatePlaylist {
    [JsonPropertyName("tracks")]
    public BookmateAudioTrack[] Tracks { get; set; }
}