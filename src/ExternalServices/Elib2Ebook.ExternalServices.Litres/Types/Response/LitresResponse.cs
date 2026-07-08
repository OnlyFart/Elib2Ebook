using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}
