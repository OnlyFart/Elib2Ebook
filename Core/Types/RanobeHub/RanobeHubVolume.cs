using System.Text.Json.Serialization;

namespace Core.Types.RanobeHub; 

public class RanobeHubVolume {
    [JsonPropertyName("chapters")]
    public RanobeHubChapter[] Chapters { get; set; }
}