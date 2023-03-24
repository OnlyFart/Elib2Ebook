using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litsovet; 

public class LitsovetLoginResponse {
    [JsonPropertyName("ok")]
    public int Ok { get; set; }
}