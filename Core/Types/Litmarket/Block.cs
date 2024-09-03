using System.Text.Json.Serialization;

namespace Core.Types.Litmarket; 

public class Block {
    [JsonPropertyName("chunk")] 
    public Chunk Chunk { get; set; }
        
    [JsonPropertyName("index")] 
    public int Index { get; set; }
}