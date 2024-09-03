using System.Text.Json.Serialization;

namespace Core.Types.Bookriver; 

public class BookRiverApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}