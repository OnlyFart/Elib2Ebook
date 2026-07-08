using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litgorod.Types;

internal class LitgorodChapter
{
    [JsonPropertyName("explodedParagraphs")]
    public string[] Paragraphs { get; set; }
}
