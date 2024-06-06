using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeLib;

public class LibSocialToken {
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}