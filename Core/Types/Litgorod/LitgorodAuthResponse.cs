using System.Text.Json.Serialization;

namespace Core.Types.Litgorod; 

public class LitgorodAuthResponse {
    [JsonPropertyName("message")]
    public string Message { get; set; }
}