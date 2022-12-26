using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Bookriver; 

public class BookRiverBook {
    [JsonPropertyName("currentGraphqlBook")]
    public BookRiverCurrentBook Book { get; set; }
}