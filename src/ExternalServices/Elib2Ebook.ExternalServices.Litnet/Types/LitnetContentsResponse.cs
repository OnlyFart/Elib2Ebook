using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litnet.Types;

internal class LitnetContentsResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}
