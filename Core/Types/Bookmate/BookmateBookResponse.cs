using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateBookResponse {
    [JsonPropertyName("book")]
    public BookmateBook Book { get; set; }
    
    [JsonPropertyName("audiobook")]
    public BookmateBook AudioBook { get; set; }
}