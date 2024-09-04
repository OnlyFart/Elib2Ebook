using System.Text.Json.Serialization;

namespace Core.Types.Rulate; 

public class RulateAuthResponse {
    [JsonPropertyName("error")]
    public string Error { get; set; }
}