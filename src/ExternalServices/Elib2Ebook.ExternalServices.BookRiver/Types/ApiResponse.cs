using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BookRiver.Types;

internal class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}
