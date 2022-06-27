using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.DarkNovels; 

public class DarkNovelsData<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
}