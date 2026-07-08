using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BookRiver.Types;

internal class ChapterContent
{
    [JsonPropertyName("content")]
    public string Content { get; set; }
}
