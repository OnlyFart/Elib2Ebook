using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.HotNovelPub.Types;

internal class HotNovelPubApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}
