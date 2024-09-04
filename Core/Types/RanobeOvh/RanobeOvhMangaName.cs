using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanobeOvhMangaName {
    [JsonPropertyName("ru")]
    public string Ru { get; set; }
}