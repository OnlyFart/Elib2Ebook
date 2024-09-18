using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateUri {
    [JsonPropertyName("Image")]
    public string Image { get; set; }
    
    [JsonPropertyName("path")]
    public string Path { get; set; }
}