using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Ranobe.Types;

internal class RanobeChapterContent
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
