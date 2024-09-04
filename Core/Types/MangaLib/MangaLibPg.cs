using System.Text.Json.Serialization;

namespace Core.Types.MangaLib; 

public class MangaLibPg {
    [JsonPropertyName("p")]
    public int P { get; set; }
    
    [JsonPropertyName("u")]
    public string U { get; set; }
}