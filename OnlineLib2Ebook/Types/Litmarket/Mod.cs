using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Litmarket {
    public class Mod {
        [JsonPropertyName("type")] 
        public string Type { get; set; }

        [JsonPropertyName("text")] 
        public string Text { get; set; }
        
        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }
        
        [JsonPropertyName("mods")]
        public Mod[] Mods { get; set; }
    }
}