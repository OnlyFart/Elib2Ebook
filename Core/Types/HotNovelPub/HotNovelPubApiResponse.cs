using System.Text.Json.Serialization;

namespace Core.Types.HotNovelPub; 

public class HotNovelPubApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}