using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobesNet; 

public class WindowData {
    [JsonPropertyName("chapters")]
    public RanobesNetChapter[] Chapters { get; set; }
    
    [JsonPropertyName("pages_count")]
    public int Pages { get; set; }
}