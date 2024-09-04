using System.Text.Json.Serialization;

namespace Core.Types.Litnet; 

public class LitnetBookResponse {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; }
    
    [JsonPropertyName("cover")]
    public string Cover { get; set; }
    
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
    
    [JsonPropertyName("author_id")]
    public long AuthorId { get; set; }
    
    [JsonPropertyName("lang")]
    public string Lang { get; set; }
    
    [JsonPropertyName("cycle_priority")]
    public int? CyclePriority { get; set; }
    
    [JsonPropertyName("adult_only")]
    public bool AdultOnly { get; set; }
    
    [JsonPropertyName("co_author_name")]
    public string CoAuthorName { get; set; }
    
    [JsonPropertyName("co_author")]
    public long? CoAuthorId { get; set; }
}