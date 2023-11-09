using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.SocialLib; 

public class User {
    [JsonPropertyName("id")]
    public long Id { get; set; }
}