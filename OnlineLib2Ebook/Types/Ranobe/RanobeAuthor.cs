using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Ranobe {
    public class RanobeAuthor {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}