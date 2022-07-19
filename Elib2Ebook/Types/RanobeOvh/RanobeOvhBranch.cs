using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeOvh; 

public class RanobeOvhBranch {
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("translators")]
    public RanoveOvhTranslator[] Translators { get; set; }
    
    [JsonPropertyName("book")]
    public RanobeOvhManga Book { get; set; }
}