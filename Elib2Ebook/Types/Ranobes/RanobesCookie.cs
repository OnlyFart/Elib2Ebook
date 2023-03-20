using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Ranobes; 

public class RanobesCookie {
    [JsonPropertyName("cookie")]
    public string Cookie { get; set; }
}