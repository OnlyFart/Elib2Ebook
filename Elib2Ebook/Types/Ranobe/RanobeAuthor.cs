using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Ranobe; 

public class RanobeAuthor {
    [JsonPropertyName("name")]
    public string Name { get; set; }
}