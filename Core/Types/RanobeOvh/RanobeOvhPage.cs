using System.Text.Json.Serialization;

namespace Core.Types.RanobeOvh; 

public class RanobeOvhPage {
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }
}