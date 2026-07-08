using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Ranobe.Types;

internal class RanobeChapterShort
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}
