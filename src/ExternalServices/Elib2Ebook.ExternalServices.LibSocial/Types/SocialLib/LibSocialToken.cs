using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;

internal class LibSocialToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}
