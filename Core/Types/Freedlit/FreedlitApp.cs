using System.Text.Json.Serialization;

namespace Core.Types.Freedlit;

public class FreedlitApp<TErr> {
    [JsonPropertyName("props")]
    public FreedlitProps<TErr> Props { get; set; }
}