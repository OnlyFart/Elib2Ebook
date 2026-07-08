using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.IGram.Types;

internal class LitexitUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}
