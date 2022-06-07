using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.AuthorToday; 

public class AuthorTodayChapter {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("sortOrder")]
    public long SortOrder { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("key")]
    public string Key { get; set; }
    
    [JsonPropertyName("IsSuccessful")]
    public bool IsSuccessful { get; set; }
}