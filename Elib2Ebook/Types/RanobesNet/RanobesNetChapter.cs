using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobesNet; 

public class RanobesNetChapter {
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("link")]
    public string Link { get; set; }
}