using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.StrokiMts.Types;

internal class StrokiMtsApiMultiResponse
{
    [JsonPropertyName("items")]
    public List<StrokiMtsMultiItem> Items { get; set; }
}
