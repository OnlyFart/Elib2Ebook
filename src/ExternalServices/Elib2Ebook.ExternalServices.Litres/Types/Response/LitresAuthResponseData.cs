using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresAuthResponseData
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; }
}
