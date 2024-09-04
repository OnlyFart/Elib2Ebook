using System.Text.Json.Serialization;

namespace Core.Types.SocialLib;

public class LibSocialToken {
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}