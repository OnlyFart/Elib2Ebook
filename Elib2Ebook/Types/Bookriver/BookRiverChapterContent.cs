using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Bookriver; 

public class BookRiverChapterContent {
    [JsonPropertyName("content")]
    public string Content { get; set; }
}