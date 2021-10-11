using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Litmarket {
    public class ModData {
        [JsonPropertyName("src")]
        public string Src { get; set; }
        
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}