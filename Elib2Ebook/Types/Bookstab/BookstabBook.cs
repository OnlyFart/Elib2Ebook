using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Bookstab; 

public class BookstabBook {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
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