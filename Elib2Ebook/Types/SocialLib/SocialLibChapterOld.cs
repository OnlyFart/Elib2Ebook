using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.SocialLib; 

public class SocialLibChapterOld {
    [JsonPropertyName("number")] 
    public string ChapterNumber { get; set; }

    [JsonPropertyName("volume")] 
    public string ChapterVolume { get; set; }

    [JsonPropertyName("name")] 
    public string ChapterName { get; set; }
    
    [JsonPropertyName("slug")]
    public string ChapterSlug { get; set; }

    public string GetName() {
        return $"Том {ChapterVolume} Глава {ChapterNumber} {ChapterName}".Trim();
    }
}