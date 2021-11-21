using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Ranobe {
    public class RanobeBook {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("author")]
        public RanobeAuthor Author { get; set; }
        
        [JsonPropertyName("verticalImages")]
        public RanobeImage[] Images { get; set; }
        
        [JsonPropertyName("chapters")]
        public RanobeChapter[] Chapters { get; set; }
    }
}