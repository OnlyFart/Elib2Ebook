using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litgorod.Types;

internal class LitgorodBookPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
