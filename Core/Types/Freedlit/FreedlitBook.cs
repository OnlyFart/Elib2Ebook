using System.Text.Json.Serialization;

namespace Core.Types.Freedlit;

public class FreedlitBook {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("cover")]
    public string Cover { get; set; }
    
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; }
    
    [JsonPropertyName("main_author")]
    public FreedlitAuthor MainAuthor { get; set; }
}