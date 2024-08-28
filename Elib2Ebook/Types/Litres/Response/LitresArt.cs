using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Response; 

public class LitresArt {
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }

    [JsonPropertyName("persons")]
    public LitresPerson<string>[] Persons { get; set; } = [];
    
    [JsonPropertyName("cover")]
    public string Cover { get; set; }

    [JsonPropertyName("sequences")]
    public LitresSequence[] Sequences { get; set; } = [];
}