using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BookRiver.Types;

internal class BookRiverBook
{
    [JsonPropertyName("currentGraphqlBook")]
    public BookRiverCurrentBook Book { get; set; }
}
