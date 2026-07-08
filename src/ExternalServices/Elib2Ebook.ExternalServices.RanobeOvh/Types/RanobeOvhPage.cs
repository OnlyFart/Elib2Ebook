using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeOvh.Types;

internal class RanobeOvhPage
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }
}
