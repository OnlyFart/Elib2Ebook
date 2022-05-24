using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litexit; 

public class LitexitChapter {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
}