using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Dreame; 

public class DreameNovel {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("author_id")]
    public string AuthorId { get; set; }
    
    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("descr")]
    public string Description { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; }
    
    [JsonPropertyName("cover_url")]
    public string Cover { get; set; }
}