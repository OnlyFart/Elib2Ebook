using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.DarkNovels.Types;

internal class DarkNovelsAuthResponse
{
    [JsonPropertyName("token")]
    public DarkNovelsAuthToken Token { get; set; }
}
