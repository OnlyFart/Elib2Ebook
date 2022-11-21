using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeOvh; 

public class RanobeOvhChapter {
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("number")]
    public decimal Number { get; set; }
    
    [JsonPropertyName("volume")]
    public int? Volume { get; set; }
    
    [JsonPropertyName("pages")]
    public RanobeOvhPage[] Pages { get; set; }

    public string FullName {
        get {
            if (!Volume.HasValue) {
                return Name;
            }
            
            var shortName = $"Том {Volume}. Глава {(int)Number}";
            return string.IsNullOrWhiteSpace(Name) ? shortName : shortName + $" - {Name}";
        }
    }
}