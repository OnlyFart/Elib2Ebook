using System.Text.Json.Serialization;

namespace Core.Types.RanobeHub; 

public class RanobeHubChapter {
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}