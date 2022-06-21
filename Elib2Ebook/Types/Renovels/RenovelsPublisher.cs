using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsPublisher {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("dir")]
    public string Dir { get; set; }
}