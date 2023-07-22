using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres.Response; 

public class LitresStaticResponse<T> {
    [JsonPropertyName("payload")]
    public LitresResponse<T> Payload { get; set; }
}