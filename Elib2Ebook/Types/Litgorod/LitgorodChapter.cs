using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litgorod; 

public class LitgorodChapter {
    [JsonPropertyName("explodedParagraphs")]
    public string[] Paragraphs { get; set; }
}