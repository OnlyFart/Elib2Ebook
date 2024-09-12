using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateCover {
    [JsonPropertyName("large")]
    public string Large { get; set; }
    
    [JsonPropertyName("small")]
    public string Small { get; set; }
}