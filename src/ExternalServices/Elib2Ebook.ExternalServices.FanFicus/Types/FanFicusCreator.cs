using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.FanFicus.Types;

internal class FanFicusCreator
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }

    [JsonPropertyName("nickName")]
    public string NickName { get; set; }
}
