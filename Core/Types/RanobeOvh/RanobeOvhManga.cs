using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanobeOvhManga {
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public RanobeOvhMangaName Name { get; set; }
    
    [JsonPropertyName("poster")]
    public string Poster { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}