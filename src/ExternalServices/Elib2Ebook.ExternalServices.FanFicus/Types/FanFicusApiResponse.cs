using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.FanFicus.Types;

internal class FanFicusApiResponse<T>
{
    [JsonPropertyName("value")]
    public T Value { get; set; }
}
