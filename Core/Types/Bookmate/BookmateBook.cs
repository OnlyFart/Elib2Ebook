using System.Linq;
using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public abstract class BookmateBookBase {
    [JsonPropertyName("uuid")]
    public string UUID { get; set; }
    
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("cover")]
    public BookmateCover Cover { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("language")]
    public string Language { get; set; }
    
    [JsonPropertyName("linked_book_uuids")]
    public string[] LinkedBooks { get; set; }
    
    [JsonPropertyName("linked_audiobook_uuids")]
    public string[] LinkedAudio { get; set; }

    public abstract BookmateAuthor GetAuthor();
}

public class BookmateBook : BookmateBookBase {
    [JsonPropertyName("authors_objects")]
    public BookmateAuthor[] AuthorsObjects { get; set; }

    public override BookmateAuthor GetAuthor() {
        return AuthorsObjects?.FirstOrDefault();
    }
}

public class BookmateComic : BookmateBookBase {
    [JsonPropertyName("authors")]
    public BookmateAuthor[] Authors { get; set; }

    public override BookmateAuthor GetAuthor() {
        return Authors?.FirstOrDefault();
    }
}