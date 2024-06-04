using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.StrokiMts;

public class StrokiMtsApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}