using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.DarkNovels; 

public class DarkNovelsChapter {
    [JsonPropertyName("id")]
    public int Id { get; set; }
        
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("payed")]
    public int Payed { get; set; }
}