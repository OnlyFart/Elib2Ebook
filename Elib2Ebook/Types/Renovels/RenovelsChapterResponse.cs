using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsChapterResponse<T> {
    [JsonPropertyName("content")]
    public T Content { get; set; }
}