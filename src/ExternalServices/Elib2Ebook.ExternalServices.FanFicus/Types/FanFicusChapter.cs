using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.FanFicus.Types;

internal class FanFicusChapter
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
