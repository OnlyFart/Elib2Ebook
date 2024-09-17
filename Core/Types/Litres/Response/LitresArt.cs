using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response; 

public class LitresArt {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("html_annotation")]
    public string Annotation { get; set; }

    [JsonPropertyName("persons")]
    public LitresPerson<long>[] Persons { get; set; } = [];
    
    [JsonPropertyName("cover_url")]
    public string Cover { get; set; }

    [JsonPropertyName("series")]
    public LitresSeria[] Sequences { get; set; } = [];
    
    [JsonPropertyName("linked_arts")]
    public LitresArt[] LinkedArts { get; set; }
    
    [JsonPropertyName("art_type")]
    public LitresArtTypeEnum ArtType { get; set; }
}