using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Dreame; 

public class DreameCatalog {
    [JsonPropertyName("pager")]
    public DreamePager Pager { get; set; }
}