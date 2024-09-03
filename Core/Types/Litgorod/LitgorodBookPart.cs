using System.Text.Json.Serialization;

namespace Core.Types.Litgorod; 

public class LitgorodBookPart {
    [JsonPropertyName("text")]
    public string Text { get; set; }
}