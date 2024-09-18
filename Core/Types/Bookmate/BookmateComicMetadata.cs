using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateComicMetadata {
    [JsonPropertyName("pages")]
    public BookmateComicPage[] Pages { get; set; }
}