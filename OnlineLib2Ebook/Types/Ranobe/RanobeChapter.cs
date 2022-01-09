using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Ranobe; 

public class RanobeChapter {
    [JsonPropertyName("content")]
    public RanobeChapterContent Content { get; set; }
}