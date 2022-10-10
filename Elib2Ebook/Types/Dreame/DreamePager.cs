using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Dreame; 

public class DreamePager {
    [JsonPropertyName("chap_list")]
    public List<DreameChapter> ChapterList { get; set; }
}