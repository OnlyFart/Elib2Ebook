using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litexit; 

public class LitexitUser {
    [JsonPropertyName("id")]
    public int Id { get; set; }
}