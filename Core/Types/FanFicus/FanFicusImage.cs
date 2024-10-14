using System.Text.Json.Serialization;

namespace Core.Types.FanFicus;

public class FanFicusImage {
    [JsonPropertyName("url")]
    public string Url { get; set; }
}