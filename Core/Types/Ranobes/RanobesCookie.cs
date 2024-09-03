using System.Text.Json.Serialization;

namespace Core.Types.Ranobes; 

public class RanobesCookie {
    [JsonPropertyName("cookie")]
    public string Cookie { get; set; }
}