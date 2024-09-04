using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.Neobook; 

public class NeobookAttachment {
    [JsonPropertyName("image")]
    public Dictionary<string, string> Cover { get; set; }
}