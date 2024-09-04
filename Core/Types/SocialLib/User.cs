using System.Text.Json.Serialization;

namespace Core.Types.SocialLib; 

public class User {
    [JsonPropertyName("id")]
    public long Id { get; set; }
}