using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Types;

internal class HotNovelPubAuthorize
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}
