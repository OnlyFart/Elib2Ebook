using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Rulate; 

public class RulateAuthResponse {
    [JsonPropertyName("error")]
    public string Error { get; set; }
}