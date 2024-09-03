using System.Text.Json.Serialization;

namespace Core.Types.WuxiaWorld; 

public class WuxiaWorldToc {
    [JsonPropertyName("post_name")]
    public string PostName { get; set; }
}