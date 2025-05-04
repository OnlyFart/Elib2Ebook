using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.SocialLib; 

public class SocialLibChapters {
    [JsonPropertyName("list")] 
    public List<SocialLibChapter> List { get; set; }
}

public class SocialLibTocVolume: object {
    public string Number { get; set; }
    public int? Start { get; set; }
    public int? End { get; set; }
}
public class SocialLibTocChapter: object {
    public int Index { get; set; }
    public string Number { get; set; }
    public string Volume { get; set; }
}