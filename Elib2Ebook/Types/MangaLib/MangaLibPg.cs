using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.MangaLib; 

public class MangaLibPg {
    [JsonPropertyName("p")]
    public int P { get; set; }
    
    [JsonPropertyName("u")]
    public string U { get; set; }
}