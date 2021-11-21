using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Ranobe {
    public class RanobeChapter {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}