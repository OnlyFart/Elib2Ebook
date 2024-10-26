using System.Text.Json.Serialization;

namespace Core.Types.BookYandex;

public class BooksYandexUri {
    [JsonPropertyName("Image")]
    public string Image { get; set; }
    
    [JsonPropertyName("path")]
    public string Path { get; set; }
}