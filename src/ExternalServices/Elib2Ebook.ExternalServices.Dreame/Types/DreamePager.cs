using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Dreame.Types;

internal class DreamePager
{
    [JsonPropertyName("chap_list")]
    public List<DreameChapter> ChapterList { get; set; }
}
