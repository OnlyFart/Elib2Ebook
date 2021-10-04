using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Litnet.Response {
    public class LitnetResponse {
        [JsonPropertyName("status")]
        public int Status { get; set; }
        
        [JsonPropertyName("data")]
        public string Data { get; set; }
        
        [JsonPropertyName("isLastPage")]
        public bool IsLastPage { get; set; }
    }
}