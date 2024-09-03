using System.Text.Json.Serialization;

namespace Core.Types.Bookstab; 

public class BookstabBook {
    [JsonPropertyName("image")]
    public string Image { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; }

    [JsonPropertyName("user")]
    public BooksnabUser User { get; set; }
    
    [JsonPropertyName("chapters_show")]
    public BookstabChapter[] ChaptersShow { get; set; }
}