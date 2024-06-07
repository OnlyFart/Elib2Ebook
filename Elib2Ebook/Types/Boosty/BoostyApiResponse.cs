using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Boosty;

public class BoostyApiResponse<T> {
    [JsonPropertyName("data")]
    public T Data { get; set; }
    
    [JsonPropertyName("extra")]
    public BoostyExtra Extra { get; set; }
}