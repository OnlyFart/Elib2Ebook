using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Core.Types.Renovels; 

public class RenovelsChapter {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("chapter")]
    public string Chapter { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("tome")]
    public int Tome { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; }
    
    [JsonPropertyName("is_paid")]
    public bool IsPaid { get; set; }
    
    [JsonPropertyName("is_bought")]
    public bool? IsBought { get; set; }
    
    [JsonPropertyName("pages")]
    public JsonArray Pages { get; set; }
    
    public string Title {
        get {
            var name = $"Том {Tome}. Глава {Chapter}";
            if (string.IsNullOrWhiteSpace(Name)) {
                return name;
            }

            return name + $" - {Name}";
        }
    }
}