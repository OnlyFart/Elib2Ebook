using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeHub; 

public class RanobeHubApiResponse {
    [JsonPropertyName("volumes")]
    public RanobeHubVolume[] Volumes { get; set; }
}