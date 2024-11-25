using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanobeOvhManga {
    [JsonPropertyName("name")]
    public RanobeOvhMangaName Name { get; set; }
    
    [JsonPropertyName("poster")]
    public string Poster { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
}