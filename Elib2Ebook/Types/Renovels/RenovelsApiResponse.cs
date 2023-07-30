using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels;

public class RenovelsApiResponse<T> {
    [JsonPropertyName("msg")]
    public string Message { get; set; }
    
    [JsonPropertyName("content")]
    public T Content { get; set; }
}