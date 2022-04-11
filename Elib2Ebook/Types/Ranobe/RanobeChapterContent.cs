using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Ranobe; 

public class RanobeChapterContent {
    [JsonPropertyName("text")]
    public string Text { get; set; }
}