using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.AuthorToday.Types;

internal class Image
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
