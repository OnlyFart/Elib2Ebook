using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.StrokiMts.Types;

internal class StrokiMtsApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}
