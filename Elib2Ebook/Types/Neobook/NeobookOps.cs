using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookOps {
    [JsonPropertyName("ops")]
    public NeoBookOp[] Ops { get; set; }
}