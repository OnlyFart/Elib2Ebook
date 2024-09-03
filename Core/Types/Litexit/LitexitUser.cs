using System.Text.Json.Serialization;

namespace Core.Types.Litexit; 

public class LitexitUser {
    [JsonPropertyName("id")]
    public int Id { get; set; }
}