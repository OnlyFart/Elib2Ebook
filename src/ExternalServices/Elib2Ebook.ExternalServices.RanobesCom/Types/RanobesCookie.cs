using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.RanobesCom.Types;

internal class RanobesCookie
{
    [JsonPropertyName("cookie")]
    public string Cookie { get; set; }
}
