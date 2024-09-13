using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response;

public class LitresFile {
    [JsonPropertyName("extension")]
    public string Extension { get; set; }
}