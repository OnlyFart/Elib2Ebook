using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.AuthorToday.Types;

internal class User
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
