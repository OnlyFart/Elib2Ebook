using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Neobook.Types;

internal class NeobookChapter
{
    [JsonPropertyName("data")]
    public NeobookChapterData Data { get; set; }
}
