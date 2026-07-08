using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.FanFicus.Types;

internal class FanFicusUser
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
}
