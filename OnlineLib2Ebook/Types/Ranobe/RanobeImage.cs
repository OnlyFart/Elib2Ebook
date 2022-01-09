using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Ranobe; 

public class RanobeImage {
    [JsonPropertyName("width")]
    public int Width { get; set; }
        
    [JsonPropertyName("height")]
    public int Height { get; set; }
        
    [JsonPropertyName("processor")]
    public string Processor { get; set; }
        
    [JsonPropertyName("url")]
    public string Url { get; set; }
}