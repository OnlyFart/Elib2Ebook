using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Bookriver; 

public class BookRiverApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}