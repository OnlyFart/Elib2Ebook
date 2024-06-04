using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeLib;

public class RanobeLibBookChapters {
    [JsonPropertyName("data")]
    public List<RanobeLibBookChapter> Chapters { get; set; }
}

public class RanobeLibBookChapterResponse {
    [JsonPropertyName("data")]
    public RanobeLibBookChapter Data { get; set; }
}

public class RanobeLibBookChapter {
    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    private string RawName { get; set; }

    [JsonPropertyName("name")]
    public string Name {
        get {
            var name = $"Том {Volume} Глава {Number}";
            if (!string.IsNullOrWhiteSpace(RawName)) {
                name += $" - {RawName}";
            }

            return name;
        }
        set { RawName = value; }
    }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}