using System.Text.Json.Serialization;

namespace Core.Types.Neobook; 

public class NeobookPostData {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("user")]
    public NeobookUser User { get; set; }
    
    [JsonPropertyName("chapters")]
    public NeobookTocChapter[] Chapters { get; set; }
    
    [JsonPropertyName("attachment")]
    public NeobookAttachment Attachment { get; set; }
}