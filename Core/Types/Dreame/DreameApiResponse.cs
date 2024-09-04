using System.Text.Json.Serialization;

namespace Core.Types.Dreame; 

public class DreameApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}