using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Freedlit;

public class FreedlitApp<TErr> {
    [JsonPropertyName("props")]
    public FreedlitProps<TErr> Props { get; set; }
}