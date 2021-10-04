using System.Text.Json.Serialization;

namespace Author.Today.Epub.Converter.Types.Litmarket {
    public class Chunk {
        [JsonPropertyName("type")] 
        public string Type { get; set; }

        [JsonPropertyName("key")] 
        public string Key { get; set; }

        [JsonPropertyName("mods")] 
        public Mod[] Mods { get; set; }
    }
}