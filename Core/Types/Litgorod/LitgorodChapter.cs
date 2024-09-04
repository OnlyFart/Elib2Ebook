using System.Text.Json.Serialization;

namespace Core.Types.Litgorod; 

public class LitgorodChapter {
    [JsonPropertyName("explodedParagraphs")]
    public string[] Paragraphs { get; set; }
}