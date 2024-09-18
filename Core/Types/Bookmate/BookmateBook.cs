using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateBook {
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("authors_objects")]
    public BookmateAuthor[] Authors { get; set; }
    
    [JsonPropertyName("cover")]
    public BookmateCover Cover { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; }
}