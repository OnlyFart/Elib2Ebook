using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response; 

public class LitresArts {
    [JsonPropertyName("arts")]
    public LitresArt[] Arts { get; set; }
}