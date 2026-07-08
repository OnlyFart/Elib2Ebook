using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.FanFicus.Types;

internal class FanFicusPart
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}
