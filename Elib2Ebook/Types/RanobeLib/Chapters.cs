using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeLib; 

public class Chapters {
    [JsonPropertyName("list")] 
    public List<Chapter> List { get; set; }
}