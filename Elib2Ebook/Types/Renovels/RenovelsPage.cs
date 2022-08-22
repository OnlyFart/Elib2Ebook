using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsPage {
    [JsonPropertyName("link")]
    public string Link { get; set; }
}