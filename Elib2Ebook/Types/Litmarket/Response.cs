using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litmarket; 

public class Response {
    [JsonPropertyName("book")] 
    public LBook Book { get; set; }

    [JsonPropertyName("tableOfContent")] 
    public string Toc { get; set; }
}