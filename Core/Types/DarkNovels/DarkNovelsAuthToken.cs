using System.Text.Json.Serialization;

namespace Core.Types.DarkNovels; 

public class DarkNovelsAuthToken {
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }
}