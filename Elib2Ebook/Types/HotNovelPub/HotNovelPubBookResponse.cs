using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.HotNovelPub; 

public class HotNovelPubBookResponse {
    [JsonPropertyName("book")]
    public HotNovelPubBook Book { get; set; }
    
    [JsonPropertyName("chapters")]
    public List<HotNovelPubChapter> Chapters { get; set; }
}