using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Litmarket {
    public class LBook {
        [JsonPropertyName("ebookId")] 
        public int EbookId { get; set; }
    }
}