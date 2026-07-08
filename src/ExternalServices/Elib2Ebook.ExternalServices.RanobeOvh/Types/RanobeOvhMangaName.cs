using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeOvh.Types;

internal class RanobeOvhMangaName
{
    [JsonPropertyName("ru")]
    public string Ru { get; set; }
}
