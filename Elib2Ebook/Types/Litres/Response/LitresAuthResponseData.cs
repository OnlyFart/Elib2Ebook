using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Response; 

public class LitresAuthResponseData {
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("sid")]
    public string Sid { get; set; }
    
    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; }
}