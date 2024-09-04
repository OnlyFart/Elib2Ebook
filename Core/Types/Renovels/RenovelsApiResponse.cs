using System.Text.Json.Serialization;

namespace Core.Types.Renovels;

public class RenovelsApiResponse<T> {
    [JsonPropertyName("msg")]
    public string Message { get; set; }
    
    [JsonPropertyName("content")]
    public T Content { get; set; }
}