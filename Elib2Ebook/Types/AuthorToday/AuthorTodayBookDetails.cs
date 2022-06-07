using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.AuthorToday; 

public class AuthorTodayBookDetails {
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }
    
    [JsonPropertyName("seriesId")]
    public long? SeriesId { get; set; }
    
    [JsonPropertyName("seriesWorkNumber")]
    public long? SeriesWorkNumber { get; set; }
    
    [JsonPropertyName("seriesTitle")]
    public string SeriesTitle { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("authorFIO")]
    public string AuthorFio { get; set; }
    
    [JsonPropertyName("authorId")]
    public long AuthorId { get; set; }
    
    [JsonPropertyName("coverUrl")]
    public string CoverUrl { get; set; }
    
    [JsonPropertyName("chapters")]
    public AuthorTodayChapter[] Chapters { get; set; }
    
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("authorUserName")]
    public string AuthorUserName { get; set; }
}