using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.MyBook.Types;

internal class MyBookAuthor
{
    [JsonPropertyName("cover_name")]
    public string Name { get; set; }

    [JsonPropertyName("absolute_url")]
    public string Url { get; set; }
}
