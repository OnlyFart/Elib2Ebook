using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Bookstab.Types;

internal class BookstabChapter
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; }
}
