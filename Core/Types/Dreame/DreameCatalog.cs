using System.Text.Json.Serialization;

namespace Core.Types.Dreame; 

public class DreameCatalog {
    [JsonPropertyName("pager")]
    public DreamePager Pager { get; set; }
}