using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Dreame; 

public class DreameApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}