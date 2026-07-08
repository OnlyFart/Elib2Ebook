using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Freedlit.Types;

internal class FreedlitProps<TErr>
{
    [JsonPropertyName("book")]
    public FreedlitBook Book { get; set; }

    [JsonPropertyName("errors")]
    public TErr Errors { get; set; }
}
