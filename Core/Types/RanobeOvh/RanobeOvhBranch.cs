using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanobeOvhBranch {
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("translators")]
    public RanoveOvhTranslator[] Translators { get; set; }
    
    [JsonPropertyName("book")]
    public RanobeOvhManga Book { get; set; }
    
    [JsonPropertyName("chaptersCount")]
    public long ChaptersCount { get; set; }
}