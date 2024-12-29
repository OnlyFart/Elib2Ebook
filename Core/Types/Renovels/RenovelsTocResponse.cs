using System.Text.Json.Serialization;

namespace Core.Types.Renovels;

public class RenovelsTocResponse {
    [JsonPropertyName("results")]
    public RenovelsChapter[] Results { get; set; }
}