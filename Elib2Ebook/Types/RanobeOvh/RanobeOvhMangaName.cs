using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeOvh; 

public class RanobeOvhMangaName {
    [JsonPropertyName("ru")]
    public string Ru { get; set; }
}