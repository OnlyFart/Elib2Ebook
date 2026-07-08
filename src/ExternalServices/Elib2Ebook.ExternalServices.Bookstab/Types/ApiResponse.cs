using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Bookstab.Types;

internal class ApiResponse
{
    [JsonPropertyName("book")]
    public BookstabBook Book { get; set; }

    [JsonPropertyName("chapter")]
    public BookstabChapter Chapter { get; set; }
}
