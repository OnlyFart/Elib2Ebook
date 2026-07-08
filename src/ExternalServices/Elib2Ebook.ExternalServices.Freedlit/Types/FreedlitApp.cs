using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Freedlit.Types;

internal class FreedlitApp<TErr>
{
    [JsonPropertyName("props")]
    public FreedlitProps<TErr> Props { get; set; }
}
