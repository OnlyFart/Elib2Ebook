using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexEpisode {
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
    
    [JsonPropertyName("can_be_read")]
    public bool CanBeRead { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
}