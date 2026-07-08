using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Types;

internal class HotNovelPubBookResponse
{
    [JsonPropertyName("book")]
    public HotNovelPubBook Book { get; set; }

    [JsonPropertyName("chapters")]
    public List<HotNovelPubChapter> Chapters { get; set; }
}
