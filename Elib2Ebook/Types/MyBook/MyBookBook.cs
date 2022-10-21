using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.MyBook; 

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
}