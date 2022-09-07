using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.DarkNovels; 

public class DarkNovelsAuthToken {
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }
}