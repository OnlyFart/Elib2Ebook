using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Bookriver; 

public class BookRiverChapterContent {
    [JsonPropertyName("content")]
    public string Content { get; set; }
}