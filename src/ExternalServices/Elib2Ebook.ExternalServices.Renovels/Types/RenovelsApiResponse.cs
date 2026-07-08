using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Renovels.Types;

internal class RenovelsApiResponse<T>
{
    [JsonPropertyName("msg")]
    public string Message { get; set; }

    [JsonPropertyName("content")]
    public T Content { get; set; }
}
