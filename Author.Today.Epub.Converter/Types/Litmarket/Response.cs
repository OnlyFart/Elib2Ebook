using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Litmarket {
    public class Response {
        [JsonPropertyName("book")] 
        public LBook Book { get; set; }

        [JsonPropertyName("tableOfContent")] 
        public string Toc { get; set; }
    }
}