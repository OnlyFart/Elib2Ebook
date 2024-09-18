using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateAudioOffline {
    [JsonPropertyName("max_bit_rate")]
    public BookmateAudioBitrate Max { get; set; }
    
    [JsonPropertyName("min_bit_rate")]
    public BookmateAudioBitrate Min { get; set; }
}