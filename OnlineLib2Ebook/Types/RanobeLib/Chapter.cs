using System;
using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.RanobeLib; 

public class Chapter {
    [JsonPropertyName("chapter_number")]
    public string ChapterNumber { get; set; }
    
    [JsonPropertyName("chapter_volume")]
    public int ChapterVolume { get; set; }
    
    [JsonPropertyName("chapter_name")]
    public string ChapterName { get; set; }

    public Uri GetUri(Uri baseUri) {
        return new Uri(baseUri + $"/v{ChapterVolume}/c{ChapterNumber}");
    }

    public string GetName() {
        return $"Том {ChapterVolume} Глава {ChapterNumber} {ChapterName}".Trim();
    }
}