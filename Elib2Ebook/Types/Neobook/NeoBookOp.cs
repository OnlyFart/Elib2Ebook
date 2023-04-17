using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeoBookOp {
    [JsonPropertyName("insert")]
    public string Insert { get; set; } 
}