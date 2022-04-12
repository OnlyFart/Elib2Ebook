using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeHub; 

public class RanobeHubVolume {
    [JsonPropertyName("chapters")]
    public RanobeHubChapter[] Chapters { get; set; }
}