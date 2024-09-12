using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateAuthor {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
}