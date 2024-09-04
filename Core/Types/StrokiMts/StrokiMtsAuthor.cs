using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsAuthor {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("friendlyUrl")]
    public string FriendlyUrl { get; set; }
}