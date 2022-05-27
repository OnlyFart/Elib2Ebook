using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litgorod; 

public class LitgorodAuthResponse {
    [JsonPropertyName("message")]
    public string Message { get; set; }
}