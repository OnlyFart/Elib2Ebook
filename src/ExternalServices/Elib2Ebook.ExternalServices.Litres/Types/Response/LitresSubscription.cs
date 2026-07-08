using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresSubscription
{
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}
