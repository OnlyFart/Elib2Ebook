using System.Text.Json.Serialization;

namespace Core.Types.Litmarket; 

public class LBook {
    [JsonPropertyName("ebookId")] 
    public int EbookId { get; set; }
}