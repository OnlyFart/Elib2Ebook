using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Response; 

public class LitresResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
    
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}