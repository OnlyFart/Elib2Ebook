using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Dreame.Types;

internal class DreameApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}
