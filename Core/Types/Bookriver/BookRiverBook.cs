using System.Text.Json.Serialization;

namespace Core.Types.Bookriver; 

public class BookRiverBook {
    [JsonPropertyName("currentGraphqlBook")]
    public BookRiverCurrentBook Book { get; set; }
}