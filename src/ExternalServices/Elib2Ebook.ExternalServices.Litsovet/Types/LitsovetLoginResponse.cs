using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litsovet.Types;

internal class LitsovetLoginResponse
{
    [JsonPropertyName("ok")]
    public int Ok { get; set; }
}
