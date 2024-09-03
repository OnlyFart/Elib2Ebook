using System.Text.Json.Serialization;

namespace Core.Types.Neobook;

public class NeobookChapterData {
    [JsonPropertyName("html")]
    public string Html { get; set; } 
}