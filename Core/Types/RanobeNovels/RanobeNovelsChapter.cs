using System.Text.Json.Serialization;

namespace Core.Types.RanobeNovels; 

public class RanobeNovelsChapter {
    [JsonPropertyName("post_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("post_title")]
    public string Title { get; set; }
}