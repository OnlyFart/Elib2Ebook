using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Ranobe; 

public class RanobeChapterContent {
    [JsonPropertyName("text")]
    public string Text { get; set; }
}