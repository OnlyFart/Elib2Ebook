using System.Text.Json.Serialization;

namespace Core.Types.Wattpad; 

public class WattpadUser {
    [JsonPropertyName("name")]
    public string Name { get; set; }
}