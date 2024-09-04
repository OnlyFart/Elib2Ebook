using System.Text.Json.Serialization;

namespace Core.Types.Litnet; 

public class LitnetChapterResponse {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
}