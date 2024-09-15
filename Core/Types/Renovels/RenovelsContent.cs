using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsContent {
    [JsonPropertyName("img")]
    public Dictionary<string, string> Img { get; set; }
    
    [JsonPropertyName("secondary_name")]
    public string SecondaryName { get; set; }
    
    [JsonPropertyName("main_name")]
    public string MainName { get; set; }
    
    [JsonPropertyName("another_name")]
    public string AnotherName { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("branches")]
    public RenovelsBranch[] Branches { get; set; }
    
    [JsonPropertyName("publishers")]
    public RenovelsPublisher[] Publishers { get; set; }
}