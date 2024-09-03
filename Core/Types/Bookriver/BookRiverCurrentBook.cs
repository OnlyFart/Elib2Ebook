using System.Text.Json.Serialization;

namespace Core.Types.Bookriver; 

public class BookRiverCurrentBook {
    [JsonPropertyName("id")]
    public long Id { get; set; }
}