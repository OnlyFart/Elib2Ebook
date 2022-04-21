using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeNovels; 

public class RanobeNovelsChapter {
    [JsonPropertyName("ID")]
    public string Id { get; set; }
    
    [JsonPropertyName("post_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("post_title")]
    public string Title { get; set; }
}