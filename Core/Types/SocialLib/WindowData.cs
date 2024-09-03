using System.Text.Json.Serialization;

namespace Core.Types.SocialLib; 

public class WindowData {
    [JsonPropertyName("chapters")] 
    public SocialLibChapters Chapters { get; set; }
    
    [JsonPropertyName("user")]
    public User User { get; set; }
}