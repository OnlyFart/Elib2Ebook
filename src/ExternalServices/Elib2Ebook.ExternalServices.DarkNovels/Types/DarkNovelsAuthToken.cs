using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.DarkNovels.Types;

internal class DarkNovelsAuthToken
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }
}
