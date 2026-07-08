using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Renovels.Types;

internal class RenovelsPage
{
    [JsonPropertyName("link")]
    public string Link { get; set; }
}
