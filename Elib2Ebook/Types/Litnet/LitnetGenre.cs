using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litnet; 

public class LitnetGenre {
    [JsonPropertyName("name")]
    public string Name { get; set; }
}