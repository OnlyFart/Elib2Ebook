using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Renovels.Types;

internal class RenovelsPublisher
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("dir")]
    public string Dir { get; set; }
}
