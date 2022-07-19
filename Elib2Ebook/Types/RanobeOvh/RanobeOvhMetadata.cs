using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeOvh; 

public class RanobeOvhMetadata {
    [JsonPropertyName("type")]
    public string Type { get; set; }
}