using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Response; 

public class LitresArts {
    [JsonPropertyName("arts")]
    public LitresArt[] Arts { get; set; }
}