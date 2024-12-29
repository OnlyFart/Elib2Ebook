using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsContent {
    [JsonPropertyName("cover")]
    public Dictionary<string, string> Cover { get; set; }
    
    [JsonPropertyName("main_name")]
    public string MainName { get; set; }
    
    [JsonPropertyName("secondary_name")]
    public string SecondaryName { get; set; }
    
    [JsonPropertyName("another_name")]
    public string AnotherName { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("branches")]
    public RenovelsBranch[] Branches { get; set; }
}