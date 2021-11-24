using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.DakrNovels {
    public class DarkNovelsChapter {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("position")]
        public int Position { get; set; }
        
        [JsonPropertyName("payed")]
        public int Payed { get; set; }
    }
}