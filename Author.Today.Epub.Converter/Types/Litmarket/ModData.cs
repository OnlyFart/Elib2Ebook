using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Litmarket {
    public class ModData {
        [JsonPropertyName("src")]
        public string Src { get; set; }
        
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}