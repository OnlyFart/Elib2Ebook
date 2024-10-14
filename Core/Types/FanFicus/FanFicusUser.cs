using System.Text.Json.Serialization;

namespace Core.Types.FanFicus;

public class FanFicusUser {
    [JsonPropertyName("token")]
    public string Token { get; set; }
}