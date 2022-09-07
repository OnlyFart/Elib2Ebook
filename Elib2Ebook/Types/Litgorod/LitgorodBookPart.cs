using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litgorod; 

public class LitgorodBookPart {
    [JsonPropertyName("text")]
    public string Text { get; set; }
}