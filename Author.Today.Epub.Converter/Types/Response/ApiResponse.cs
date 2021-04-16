using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Response {
    public class ApiResponse<T> {
        [JsonPropertyName("isSuccessful")]
        public bool IsSuccessful { get; set; }
        
        [JsonPropertyName("messages")]
        public string[] Messages { get; set; }
        
        public T Data { get; set; }
    }
}
