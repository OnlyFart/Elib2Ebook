using System.Text.Json.Serialization;

namespace Core.Types.FanFicus;

public class FanFicusChapter {
    [JsonPropertyName("text")]
    public string Text { get; set; }
}