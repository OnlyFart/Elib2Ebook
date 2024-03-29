using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Response; 

public class LitresSequence {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("sequence_number")]
    public string SequenceNumber { get; set; }
}