using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresFile
{
    [JsonPropertyName("extension")]
    public string Extension { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }
}
