using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook;

public class NeobookChapterData {
    [JsonPropertyName("html")]
    public string Html { get; set; } 
}