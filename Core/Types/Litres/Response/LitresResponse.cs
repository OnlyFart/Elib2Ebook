using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response; 

public class LitresResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}