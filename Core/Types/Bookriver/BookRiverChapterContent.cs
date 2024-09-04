using System.Text.Json.Serialization;

namespace Core.Types.Bookriver; 

public class BookRiverChapterContent {
    [JsonPropertyName("content")]
    public string Content { get; set; }
}