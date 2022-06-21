using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsBranch {
    [JsonPropertyName("id")]
    public int Id { get; set; }
}