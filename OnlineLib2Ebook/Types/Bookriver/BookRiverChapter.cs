using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Bookriver; 

public class BookRiverChapter {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}