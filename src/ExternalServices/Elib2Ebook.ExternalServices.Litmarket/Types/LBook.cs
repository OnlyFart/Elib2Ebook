using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litmarket.Types;

internal class LBook
{
    [JsonPropertyName("ebookId")]
    public int EbookId { get; set; }
}
