using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litmarket.Types;

internal class Chunk
{
    [JsonPropertyName("mods")]
    public Mod[] Mods { get; set; }
}
