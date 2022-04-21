using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litnet; 

public class LitnetChapterResponse {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
}