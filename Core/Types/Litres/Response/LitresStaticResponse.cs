using System.Text.Json.Serialization;

namespace Core.Types.Litres.Response; 

public class LitresStaticResponse<T> {
    [JsonPropertyName("payload")]
    public LitresResponse<T> Payload { get; set; }
}