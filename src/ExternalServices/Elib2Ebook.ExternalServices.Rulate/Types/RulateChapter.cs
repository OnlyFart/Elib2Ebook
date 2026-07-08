using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Rulate.Types;

internal class RulateChapter
{
    [JsonPropertyName("can_read")]
    public bool CanRead { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}
