using System.Text.Json.Serialization;

namespace Core.Types.MyBook; 

public class MyBookAuthor {
    [JsonPropertyName("cover_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("absolute_url")]
    public string Url { get; set; }
}