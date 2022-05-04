using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Bookriver; 

public class BookRiverAuthResponse {
    [JsonPropertyName("token")]
    public string Token { get; set; }
}