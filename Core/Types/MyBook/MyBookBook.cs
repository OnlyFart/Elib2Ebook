using System.Text.Json.Serialization;

namespace Core.Types.MyBook; 

public class MyBookBook {
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("bookfile")]
    public string BookFile { get; set; }
    
    [JsonPropertyName("default_cover")]
    public string Cover { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("authors")]
    public MyBookAuthor[] Authors { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
}