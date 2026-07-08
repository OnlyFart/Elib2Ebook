using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobeHub.Types;

internal class RanobeHubApiResponse
{
    [JsonPropertyName("volumes")]
    public RanobeHubVolume[] Volumes { get; set; }
}
