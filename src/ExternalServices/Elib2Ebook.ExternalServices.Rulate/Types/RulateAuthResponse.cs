using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Rulate.Types;

internal class RulateAuthResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }
}
