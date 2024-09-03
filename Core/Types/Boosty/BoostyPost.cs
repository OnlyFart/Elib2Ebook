using System;
using System.Text.Json.Serialization;

namespace Core.Types.Boosty;

public class BoostyPost {
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("hasAccess")]
    public bool HasAccess { get; set; }
}