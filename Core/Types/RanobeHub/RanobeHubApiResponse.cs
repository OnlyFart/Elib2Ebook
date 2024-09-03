using System.Text.Json.Serialization;

namespace Core.Types.RanobeHub; 

public class RanobeHubApiResponse {
    [JsonPropertyName("volumes")]
    public RanobeHubVolume[] Volumes { get; set; }
}