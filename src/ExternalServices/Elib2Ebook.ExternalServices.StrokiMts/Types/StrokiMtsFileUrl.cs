using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.StrokiMts.Types;

internal class StrokiMtsFileUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
