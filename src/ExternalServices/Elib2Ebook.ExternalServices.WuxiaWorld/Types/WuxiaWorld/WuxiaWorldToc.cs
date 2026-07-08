using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.WuxiaWorld.Types.WuxiaWorld;

internal class WuxiaWorldToc
{
    [JsonPropertyName("post_name")]
    public string PostName { get; set; }
}
