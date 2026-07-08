using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Neobook.Types;

internal class NeobookLogin
{
    [JsonPropertyName("utoken")]
    public string Utoken { get; set; }

    [JsonPropertyName("uid")]
    public string Uid { get; set; }
}
