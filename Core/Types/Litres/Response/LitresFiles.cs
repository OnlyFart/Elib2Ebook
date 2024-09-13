using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response;

public class LitresFiles {
    [JsonPropertyName("files")]
    public LitresFile[] Files { get; set; }
}