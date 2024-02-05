using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook;

public class NeobookChapter {
    [JsonPropertyName("data")]
    public NeobookChapterData Data { get; set; }
}