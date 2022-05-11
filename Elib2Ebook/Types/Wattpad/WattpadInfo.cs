using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Wattpad; 

public class WattpadInfo {
    [JsonPropertyName("author")]
    public string Author { get; set; }
    
    [JsonPropertyName("cover")]
    public string Cover { get; set; }
    
    [JsonPropertyName("group")]
    public WattpadGroup[] Group { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
}