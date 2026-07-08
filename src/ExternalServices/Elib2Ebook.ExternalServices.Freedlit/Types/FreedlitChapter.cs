using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Freedlit.Types;

internal class FreedlitChapter
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("header")]
    public string Header { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}
