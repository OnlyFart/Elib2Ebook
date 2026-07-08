using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litmarket.Types;

internal class Response
{
    [JsonPropertyName("book")]
    public LBook Book { get; set; }

    [JsonPropertyName("tableOfContent")]
    public string Toc { get; set; }
}
