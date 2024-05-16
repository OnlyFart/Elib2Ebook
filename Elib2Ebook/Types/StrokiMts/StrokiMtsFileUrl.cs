using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.StrokiMts;

public class StrokiMtsFileUrl {
    [JsonPropertyName("url")]
    public string Url { get; set; }
}