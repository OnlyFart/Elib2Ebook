using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Ranobe.Types;

internal class RanobeChapter
{
    [JsonPropertyName("content")]
    public RanobeChapterContent Content { get; set; }
}
