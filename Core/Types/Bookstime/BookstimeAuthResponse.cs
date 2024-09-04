using System.Text.Json.Serialization;

namespace Core.Types.Bookstime; 

public class BookstimeAuthResponse {
    [JsonPropertyName("X_OCTOBER_ERROR_MESSAGE")]
    public string XOctoberErrorMessage { get; set; }
}