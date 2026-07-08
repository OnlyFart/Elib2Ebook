using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.FanFicus.Types;

internal class FanFicusBook
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("creatorId")]
    public FanFicusCreator[] Creators { get; set; }

    [JsonPropertyName("images")]
    public FanFicusImage[] Images { get; set; }
}
