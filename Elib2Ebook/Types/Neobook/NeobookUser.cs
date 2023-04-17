using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Neobook; 

public class NeobookUser {
    [JsonPropertyName("username")]
    public string UserName { get; set; }
    
    [JsonPropertyName("firstname")]
    public string FirstName { get; set; }
    
    [JsonPropertyName("lastname")]
    public string LastName { get; set; }
    
    [JsonPropertyName("cover")]
    public Dictionary<string, string> Cover { get; set; }
}