using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Renovels.Types;

internal class RenovelsBranch
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("publishers")]
    public RenovelsPublisher[] Publishers { get; set; }
}
