using System.Text.Json.Serialization;

namespace Core.Types.Freedlit;

public class FreedlitProps<TErr> {
    [JsonPropertyName("book")]
    public FreedlitBook Book { get; set; }
    
    [JsonPropertyName("errors")]
    public TErr Errors { get; set; }
}