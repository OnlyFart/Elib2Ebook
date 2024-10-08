using System.Text.Json.Serialization;

namespace Core.Types.Neobook; 

public class NeobookUser {
    [JsonPropertyName("username")]
    public string UserName { get; set; }
    
    [JsonPropertyName("firstname")]
    public string FirstName { get; set; }
    
    [JsonPropertyName("lastname")]
    public string LastName { get; set; }
}