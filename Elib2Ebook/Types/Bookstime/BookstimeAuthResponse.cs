using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Bookstime; 

public class BookstimeAuthResponse {
    [JsonPropertyName("X_OCTOBER_ERROR_MESSAGE")]
    public string XOctoberErrorMessage { get; set; }
}