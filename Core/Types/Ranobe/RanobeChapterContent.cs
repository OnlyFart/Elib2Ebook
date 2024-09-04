using System.Text.Json.Serialization;

namespace Core.Types.Ranobe; 

public class RanobeChapterContent {
    [JsonPropertyName("text")]
    public string Text { get; set; }
}