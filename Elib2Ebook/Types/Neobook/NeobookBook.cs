using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook;

public class NeobookBook {
    [JsonPropertyName("chapters")]
    public NeobookChapter[] Chapters { get; set; }
    
    [JsonPropertyName("active_chapter_index")]
    public int ActiveChapterIndex { get; set; }
}