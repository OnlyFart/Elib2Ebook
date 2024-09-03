using System.Text.Json.Serialization;

namespace Core.Types.HotNovelPub; 

public class HotNovelPubChapter {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}