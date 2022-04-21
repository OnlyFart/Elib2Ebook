using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litnet; 

public class LitnetBookResponse {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; }
    
    [JsonPropertyName("cover")]
    public string Cover { get; set; }
}