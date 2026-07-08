using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeHub.Types;

internal class RanobeHubChapter
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}
