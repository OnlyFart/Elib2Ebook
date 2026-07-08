using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Boosty.Types;

internal class BoostyExtra
{
    [JsonPropertyName("isLast")]
    public bool IsLast { get; set; }

    [JsonPropertyName("offset")]
    public string Offset { get; set; }
}
