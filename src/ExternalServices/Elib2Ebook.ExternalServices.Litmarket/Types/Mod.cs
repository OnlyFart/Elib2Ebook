using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litmarket.Types;

internal class Mod
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("styles")]
    public string[] Styles { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("mods")]
    public Mod[] Mods { get; set; }
}
