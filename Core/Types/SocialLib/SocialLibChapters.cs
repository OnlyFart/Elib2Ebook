using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.SocialLib; 

public class SocialLibChapters {
    [JsonPropertyName("list")] 
    public List<SocialLibChapter> List { get; set; }
}