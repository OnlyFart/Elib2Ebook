using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Bookstab; 

public class BookstabChapter {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("body")]
    public string Body { get; set; }
}