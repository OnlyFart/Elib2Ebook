using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litnet; 

public class LitnetBookResponse {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; }
    
    [JsonPropertyName("cover")]
    public string Cover { get; set; }
    
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("genres")]
    public IEnumerable<LitnetGenre> Genres { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; }
    
    [JsonPropertyName("author_id")]
    public long AuthorId { get; set; }
}