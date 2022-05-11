using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeLib; 

public class RanobeLibChapters {
    [JsonPropertyName("list")] 
    public List<RanobeLibChapter> List { get; set; }
}