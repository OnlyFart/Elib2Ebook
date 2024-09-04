using System.Text.Json.Serialization;

namespace Core.Types.Litsovet; 

public class LitsovetLoginResponse {
    [JsonPropertyName("ok")]
    public int Ok { get; set; }
}