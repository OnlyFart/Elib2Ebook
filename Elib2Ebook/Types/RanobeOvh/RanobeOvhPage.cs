using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeOvh; 

public class RanobeOvhPage {
    [JsonPropertyName("Text")]
    public string Text { get; set; }
    
    [JsonPropertyName("image")]
    public string Image { get; set; }
    
    [JsonPropertyName("metadata")]
    public RanobeOvhMetadata Metadata { get; set; }
}