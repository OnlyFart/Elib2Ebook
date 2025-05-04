using System.Text.Json.Serialization;

namespace Core.Types.Renovels;

public class RenovelsTocResponse {
    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("results")]
    public RenovelsChapter[] Results { get; set; }
}