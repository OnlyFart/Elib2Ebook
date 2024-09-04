using System.Text.Json.Serialization;

namespace Core.Types.DarkNovels; 

public class DarkNovelsData<T> {
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; }
    
    [JsonPropertyName("data")]
    public T Data { get; set; }
}