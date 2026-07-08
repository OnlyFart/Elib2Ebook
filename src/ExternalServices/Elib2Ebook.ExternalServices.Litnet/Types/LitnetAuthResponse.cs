using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litnet.Types;

internal class LitnetAuthResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; }
}
