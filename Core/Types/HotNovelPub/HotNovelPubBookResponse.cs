using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.HotNovelPub; 

public class HotNovelPubBookResponse {
    [JsonPropertyName("book")]
    public HotNovelPubBook Book { get; set; }
    
    [JsonPropertyName("chapters")]
    public List<HotNovelPubChapter> Chapters { get; set; }
}