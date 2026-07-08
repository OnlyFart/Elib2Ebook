using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Neobook.Types;

internal class NeobookAttachment
{
    [JsonPropertyName("image")]
    public Dictionary<string, string> Cover { get; set; }
}
