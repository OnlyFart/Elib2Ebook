using System.Text.Json.Serialization;

namespace Core.Types.FanFicus;

public class FanFicusApiResponse<T> {
    [JsonPropertyName("value")]
    public T Value { get; set; }
}