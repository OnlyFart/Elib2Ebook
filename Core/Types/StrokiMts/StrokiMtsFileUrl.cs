using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsFileUrl {
    [JsonPropertyName("url")]
    public string Url { get; set; }
}