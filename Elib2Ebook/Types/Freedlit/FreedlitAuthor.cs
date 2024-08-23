using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Freedlit;

public class FreedlitAuthor {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("user_link")]
    public string UserLink { get; set; } 
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}