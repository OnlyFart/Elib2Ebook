using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.HotNovelPub; 

public class HotNovelPubBook {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("authorize")]
    public HotNovelPubAuthorize Authorize { get; set; }
    
    [JsonPropertyName("image")]
    public string Image { get; set; }
}