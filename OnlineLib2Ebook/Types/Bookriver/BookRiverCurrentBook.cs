using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Bookriver; 

public class BookRiverCurrentBook {
    [JsonPropertyName("id")]
    public long Id { get; set; }
}