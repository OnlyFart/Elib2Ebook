using System.Text.Json.Serialization;

namespace Core.Types.Bookriver; 

public class BookRiverChapter {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}