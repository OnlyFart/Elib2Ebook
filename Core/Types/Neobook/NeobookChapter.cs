using System.Text.Json.Serialization;

namespace Core.Types.Neobook;

public class NeobookChapter {
    [JsonPropertyName("data")]
    public NeobookChapterData Data { get; set; }
}