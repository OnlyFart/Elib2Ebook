using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsBranch {
    [JsonPropertyName("id")]
    public int Id { get; set; }
}