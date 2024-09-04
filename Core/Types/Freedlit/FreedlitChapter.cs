using System.Text.Json.Serialization;

namespace Core.Types.Freedlit;

public class FreedlitChapter {
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("header")]
    public string Header { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
}