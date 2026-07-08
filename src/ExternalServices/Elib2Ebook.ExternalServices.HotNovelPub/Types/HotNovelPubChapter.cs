using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Types;

internal class HotNovelPubChapter
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}
