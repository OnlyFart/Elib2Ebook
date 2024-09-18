using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateComicPage {
    [JsonPropertyName("content")]
    public BookmateComicContent Content { get; set; }
}