using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Librebook.Types;

internal class LibrebookAuthResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }
}
