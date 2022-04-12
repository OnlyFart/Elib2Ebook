using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeHub; 

public class RanobeHubChapter {
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}