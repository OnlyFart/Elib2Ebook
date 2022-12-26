using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeLib; 

public class User {
    [JsonPropertyName("id")]
    public long Id { get; set; }
}