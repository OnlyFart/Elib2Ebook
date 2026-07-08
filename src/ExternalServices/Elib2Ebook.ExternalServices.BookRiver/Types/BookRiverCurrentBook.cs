using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BookRiver.Types;

internal class BookRiverCurrentBook
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
