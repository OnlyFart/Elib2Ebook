using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Wattpad; 

public class WattpadUser {
    [JsonPropertyName("name")]
    public string Name { get; set; }
}