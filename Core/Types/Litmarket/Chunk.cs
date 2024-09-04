using System.Text.Json.Serialization;

namespace Core.Types.Litmarket; 

public class Chunk {
    [JsonPropertyName("mods")] 
    public Mod[] Mods { get; set; }
}