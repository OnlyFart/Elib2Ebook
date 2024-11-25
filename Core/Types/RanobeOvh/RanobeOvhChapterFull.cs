using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanobeOvhChapterFull {
    [JsonPropertyName("pages")]
    public RanobeOvhPage[] Pages { get; set; }
    
    [JsonPropertyName("content")]
    public JsonNode Content { get; set; }
}


public class RanobeOvhChapterShort {
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("number")]
    public decimal Number { get; set; }
    
    [JsonPropertyName("volume")]
    public int? Volume { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("branchId")]
    public string BranchId { get; set; }

    public string FullName {
        get {
            if (!Volume.HasValue) {
                return Title;
            }
            
            var shortName = $"Том {Volume}. Глава {(int)Number}";
            return string.IsNullOrWhiteSpace(Title) ? shortName : Title;
        }
    }
}

public class RanobeOvhChapterContent {
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("marks")]
    public RanobeOvhChapterMark[] Marks { get; set; } = [];

    [JsonPropertyName("content")]
    public RanobeOvhChapterContent[] Content { get; set; } = [];

    [JsonPropertyName("attrs")]
    public Dictionary<string, JsonNode> Attrs { get; set; } = new();
}

public class RanobeOvhChapterMark {
    [JsonPropertyName("type")]
    public string Type { get; set; }
}
