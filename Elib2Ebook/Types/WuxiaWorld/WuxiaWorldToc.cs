using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.WuxiaWorld; 

public class WuxiaWorldToc {
    [JsonPropertyName("post_name")]
    public string PostName { get; set; }
}