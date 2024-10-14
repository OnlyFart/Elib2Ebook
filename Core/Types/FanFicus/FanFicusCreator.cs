using System.Text.Json.Serialization;

namespace Core.Types.FanFicus;

public class FanFicusCreator {
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    
    [JsonPropertyName("nickName")]
    public string NickName { get; set; }
}