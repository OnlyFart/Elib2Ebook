using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Boosty.Types;

internal class BoostyPost
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("hasAccess")]
    public bool HasAccess { get; set; }
}
