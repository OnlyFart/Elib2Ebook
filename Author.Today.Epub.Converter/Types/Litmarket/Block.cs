using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Litmarket {
    public class Block {
        [JsonPropertyName("type")] 
        public string Type { get; set; }

        [JsonPropertyName("chunk")] 
        public Chunk Chunk { get; set; }
        
        [JsonPropertyName("index")] 
        public int Index { get; set; }
    }
}