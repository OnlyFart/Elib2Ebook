using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litnet; 

public class LitnetContentsResponse {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("pages_count")]
    public int PagesCount { get; set; }
}