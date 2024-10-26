using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexResponse {
    [JsonPropertyName("book")]
    public BooksYandexBook Book { get; set; }
    
    [JsonPropertyName("audiobook")]
    public BooksYandexBook AudioBook { get; set; }
    
    [JsonPropertyName("comicbook")]
    public BookmateComic Comicbook { get; set; }
}