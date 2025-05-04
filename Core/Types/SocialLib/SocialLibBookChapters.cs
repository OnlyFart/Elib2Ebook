using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Core.Types.SocialLib;

public class SocialLibBookChapters {
    [JsonPropertyName("data")]
    public List<SocialLibBookChapter> Chapters { get; set; }
}

public class SocialLibBookChapterResponse {
    [JsonPropertyName("data")]
    public SocialLibBookChapter Data { get; set; }
}

public class SocialLibChapterBranch {
    [JsonPropertyName("branch_id")]
    public long? BranchId { get; set; }
}

public class SocialLibChapterAttachment {
    [JsonPropertyName("url")]
    public string Url { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class SocialLibBookChapter {
    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    [JsonPropertyName("item_number")]
    public int ItemNumber { get; set; }
    
    [JsonPropertyName("branches")]
    public List<SocialLibChapterBranch> Branches { get; set; }

    [JsonPropertyName("attachments")]
    public SocialLibChapterAttachment[] Attachments { get; set; } = [];

    [JsonPropertyName("pages")]
    public SocialLibPage[] Pages { get; set; } = [];
    
    [JsonPropertyName("content")]
    public JsonNode Content { get; set; }
 
    private string RawName { get; set; }

    [JsonPropertyName("name")]
    public string Name {
        get {
            var name = $"Том {Volume} Глава {Number}";
            if (!string.IsNullOrWhiteSpace(RawName)) {
                name += $" - {RawName}";
            }

            return name;
        }
        set { RawName = value; }
    }
}

public class SocialLibChapterContent {
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("marks")]
    public SocialLibChapterMark[] Marks { get; set; } = [];

    [JsonPropertyName("content")]
    public SocialLibChapterContent[] Content { get; set; } = [];

    [JsonPropertyName("attrs")]
    public Dictionary<string, JsonNode> Attrs { get; set; } = new();
}

public class SocialLibChapterMark {
    [JsonPropertyName("type")]
    public string Type { get; set; }
}

public class SocialLibPage {
    [JsonPropertyName("url")]
    public string Url { get; set; }
}