using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsApiResponse<T> {
    [JsonPropertyName("content")]
    public T Content { get; set; }
}