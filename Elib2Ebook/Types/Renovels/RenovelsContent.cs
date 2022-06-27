using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsContent {
    [JsonPropertyName("img")]
    public Dictionary<string, string> Img { get; set; }
    
    [JsonPropertyName("rus_name")]
    public string RusName { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("branches")]
    public RenovelsBranch[] Branches { get; set; }
    
    [JsonPropertyName("publishers")]
    public RenovelsPublisher[] Publishers { get; set; }
}