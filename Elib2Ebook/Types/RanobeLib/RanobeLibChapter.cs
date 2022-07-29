using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeLib; 

public class RanobeLibChapter {
    [JsonPropertyName("chapter_number")] 
    public string ChapterNumber { get; set; }

    [JsonPropertyName("chapter_volume")] 
    public int ChapterVolume { get; set; }

    [JsonPropertyName("chapter_name")] 
    public string ChapterName { get; set; }
    
    [JsonPropertyName("chapter_id")]
    public int ChapterId { get; set; }
        
    [JsonPropertyName("branch_id")]
    public int? BranchId { get; set; }

    public string GetName() {
        return $"Том {ChapterVolume} Глава {ChapterNumber} {ChapterName}".Trim();
    }
}