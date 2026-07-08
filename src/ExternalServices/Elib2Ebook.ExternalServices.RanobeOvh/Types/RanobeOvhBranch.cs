using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeOvh.Types;

internal class RanobeOvhBranch
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("translators")]
    public RanoveOvhTranslator[] Translators { get; set; }

    [JsonPropertyName("chaptersCount")]
    public long ChaptersCount { get; set; }
}
