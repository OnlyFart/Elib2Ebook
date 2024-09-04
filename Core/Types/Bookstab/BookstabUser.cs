using System.Text.Json.Serialization;

namespace Core.Types.Bookstab; 

public class BooksnabUser {
    [JsonPropertyName("pseudonym")]
    public string Pseudonym { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}