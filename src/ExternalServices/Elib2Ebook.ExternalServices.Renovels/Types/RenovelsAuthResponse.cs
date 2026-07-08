using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Renovels.Types;

internal class RenovelsAuthResponse
{
    [JsonPropertyName("access_token")]
    public string Token { get; set; }
}
