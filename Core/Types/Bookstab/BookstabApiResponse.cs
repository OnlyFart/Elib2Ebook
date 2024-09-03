using System.Text.Json.Serialization;

namespace Core.Types.Bookstab; 

public class BookstabApiResponse {
    [JsonPropertyName("book")]
    public BookstabBook Book { get; set; }
    
    [JsonPropertyName("chapter")]
    public BookstabChapter Chapter { get; set; }
}