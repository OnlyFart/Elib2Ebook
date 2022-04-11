using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Ranobe; 

public class RanobeChapterShort {
    [JsonPropertyName("title")]
    public string Title { get; set; }
        
    [JsonPropertyName("url")]
    public string Url { get; set; }
}