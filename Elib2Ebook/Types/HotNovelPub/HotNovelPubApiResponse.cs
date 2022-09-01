using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.HotNovelPub; 

public class HotNovelPubApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}