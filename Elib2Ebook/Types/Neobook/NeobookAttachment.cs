using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookAttachment {
    [JsonPropertyName("image")]
    public Dictionary<string, string> Cover { get; set; }
}