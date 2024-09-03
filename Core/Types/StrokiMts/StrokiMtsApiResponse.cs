using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}