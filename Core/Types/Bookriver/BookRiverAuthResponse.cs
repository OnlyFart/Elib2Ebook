using System.Text.Json.Serialization;

namespace Core.Types.Bookriver; 

public class BookRiverAuthResponse {
    [JsonPropertyName("token")]
    public string Token { get; set; }
}