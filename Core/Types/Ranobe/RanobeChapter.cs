using System.Text.Json.Serialization;

namespace Core.Types.Ranobe; 

public class RanobeChapter {
    [JsonPropertyName("content")]
    public RanobeChapterContent Content { get; set; }
}