using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Bookstab.Types;

internal class BooksnabUser
{
    [JsonPropertyName("pseudonym")]
    public string Pseudonym { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
