using System.Text.Json.Serialization;

namespace Core.Types.Freedlit;

public class FreedlitApiResponse<T> {
    [JsonPropertyName("success")]
    public T Success { get; set; }
}