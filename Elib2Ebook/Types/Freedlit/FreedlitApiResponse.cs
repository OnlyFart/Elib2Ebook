using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Freedlit;

public class FreedlitApiResponse<T> {
    [JsonPropertyName("success")]
    public T Success { get; set; }
}