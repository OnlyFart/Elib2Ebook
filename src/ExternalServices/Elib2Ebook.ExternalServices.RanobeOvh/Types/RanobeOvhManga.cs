using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeOvh.Types;

internal class RanobeOvhManga
{
    [JsonPropertyName("name")]
    public RanobeOvhMangaName Name { get; set; }

    [JsonPropertyName("poster")]
    public string Poster { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}
