using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Neobook.Types;

internal class NeobookChapterData
{
    [JsonPropertyName("html")]
    public string Html { get; set; }
}
