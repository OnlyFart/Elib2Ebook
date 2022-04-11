using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Ranobe; 

public class RanobeChapter {
    [JsonPropertyName("content")]
    public RanobeChapterContent Content { get; set; }
}