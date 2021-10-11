using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Litmarket {
    public class LBook {
        [JsonPropertyName("ebookId")] 
        public int EbookId { get; set; }
    }
}