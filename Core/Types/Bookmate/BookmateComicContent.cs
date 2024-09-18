using System.Text.Json.Serialization;

namespace Core.Types.Bookmate;

public class BookmateComicContent {
    [JsonPropertyName("uris")]
    public BookmateUri Uri { get; set; }
}