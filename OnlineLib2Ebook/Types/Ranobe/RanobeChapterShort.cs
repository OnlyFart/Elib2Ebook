using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Ranobe; 

public class RanobeChapterShort {
    [JsonPropertyName("title")]
    public string Title { get; set; }
        
    [JsonPropertyName("url")]
    public string Url { get; set; }
}