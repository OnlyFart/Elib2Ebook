using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.HotNovelPub; 

public class HotNovelPubAuthorize {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}