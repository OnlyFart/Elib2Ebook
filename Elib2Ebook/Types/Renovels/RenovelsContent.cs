using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Renovels; 

public class RenovelsContent {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("img")]
    public Dictionary<string, string> Img { get; set; }
    
    [JsonPropertyName("rus_name")]
    public string RusName { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("count_chapters")]
    public int CountChapters { get; set; }
    
    [JsonPropertyName("branches")]
    public RenovelsBranch[] Branches { get; set; }
}