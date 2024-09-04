using System.Text.Json.Serialization;

namespace Core.Types.Rulate; 

public class RulateChapter {
    [JsonPropertyName("can_read")]
    public bool CanRead { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
}