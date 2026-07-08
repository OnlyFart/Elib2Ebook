using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.MyBook.Types;

internal class MyBookFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}
