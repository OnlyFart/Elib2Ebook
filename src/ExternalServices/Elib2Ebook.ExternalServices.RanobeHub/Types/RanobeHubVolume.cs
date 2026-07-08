using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeHub.Types;

internal class RanobeHubVolume
{
    [JsonPropertyName("chapters")]
    public RanobeHubChapter[] Chapters { get; set; }
}
