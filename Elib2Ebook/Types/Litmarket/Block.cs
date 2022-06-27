using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litmarket; 

public class Block {
    [JsonPropertyName("chunk")] 
    public Chunk Chunk { get; set; }
        
    [JsonPropertyName("index")] 
    public int Index { get; set; }
}