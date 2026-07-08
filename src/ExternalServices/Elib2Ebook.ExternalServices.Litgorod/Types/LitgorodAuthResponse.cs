using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litgorod.Types;

internal class LitgorodAuthResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; }
}
