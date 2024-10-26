using System.Linq;
using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public abstract class BookmateBookBase {
    [JsonPropertyName("uuid")]
    public string UUID { get; set; }
    
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("cover")]
    public BooksYandexCover Cover { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; }
    
    [JsonPropertyName("linked_book_uuids")]
    public string[] LinkedBooks { get; set; }
    
    [JsonPropertyName("linked_audiobook_uuids")]
    public string[] LinkedAudio { get; set; }
    
    [JsonPropertyName("source_type")]
    public string SourceType { get; set; }

    public abstract BooksYandexAuthor GetAuthor();
}

public class BooksYandexBook : BookmateBookBase {
    [JsonPropertyName("authors_objects")]
    public BooksYandexAuthor[] AuthorsObjects { get; set; }

    public override BooksYandexAuthor GetAuthor() {
        return AuthorsObjects?.FirstOrDefault();
    }
}

public class BookmateComic : BookmateBookBase {
    [JsonPropertyName("authors")]
    public BooksYandexAuthor[] Authors { get; set; }

    public override BooksYandexAuthor GetAuthor() {
        return Authors?.FirstOrDefault();
    }
}