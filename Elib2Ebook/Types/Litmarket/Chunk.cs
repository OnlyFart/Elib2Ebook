using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litmarket; 

public class Chunk {
    [JsonPropertyName("mods")] 
    public Mod[] Mods { get; set; }
}