using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Litmarket {
    public class Response {
        [JsonPropertyName("blocks")] 
        public Block[] Blocks { get; set; }

        [JsonPropertyName("book")] 
        public LBook Book { get; set; }

        [JsonPropertyName("tableOfContent")] 
        public string Toc { get; set; }
    }
}