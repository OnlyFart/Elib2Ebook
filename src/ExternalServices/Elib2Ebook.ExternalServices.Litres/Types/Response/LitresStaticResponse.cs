using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresStaticResponse<T>
{
    [JsonPropertyName("errors")]
    public string Error { get; set; }

    [JsonPropertyName("payload")]
    public LitresResponse<T> Payload { get; set; }
}
