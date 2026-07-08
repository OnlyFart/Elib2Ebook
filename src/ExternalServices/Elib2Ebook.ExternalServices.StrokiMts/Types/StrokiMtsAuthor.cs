using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.StrokiMts.Types;

internal class StrokiMtsAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("friendlyUrl")]
    public string FriendlyUrl { get; set; }
}
