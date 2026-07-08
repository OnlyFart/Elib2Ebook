using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.FanFicus.Types;

internal class FanFicusImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
