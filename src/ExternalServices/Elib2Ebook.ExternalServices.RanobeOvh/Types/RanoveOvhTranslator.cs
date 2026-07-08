using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeOvh.Types;

internal class RanoveOvhTranslator
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}
