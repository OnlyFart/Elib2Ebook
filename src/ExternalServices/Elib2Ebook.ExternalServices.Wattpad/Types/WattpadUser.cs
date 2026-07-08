using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Wattpad.Types;

internal class WattpadUser
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
