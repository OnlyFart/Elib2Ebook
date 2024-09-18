using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateAudioBitrate {
    [JsonPropertyName("url")]
    public string Url { get; set; }
}