using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsBookItem {
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("imageUrl")]
    public Dictionary<string, string> ImageUrl { get; set; }
    
    [JsonPropertyName("authors")]
    public List<StrokiMtsAuthor> Authors { get; set; }
}