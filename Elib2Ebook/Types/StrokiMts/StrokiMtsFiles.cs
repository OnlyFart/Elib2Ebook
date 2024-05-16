using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.StrokiMts;

public class StrokiMtsFiles {
    [JsonPropertyName("preview")]
    public StrokiMtsFile Preview { get; set; }
    
    [JsonPropertyName("full")]
    public StrokiMtsFile[] Full { get; set; }
}