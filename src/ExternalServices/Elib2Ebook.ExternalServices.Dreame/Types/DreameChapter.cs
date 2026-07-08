using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Dreame.Types;

internal class DreameChapter
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}
