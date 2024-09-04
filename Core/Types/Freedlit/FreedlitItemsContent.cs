using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.Freedlit;

public class FreedlitItemsContent<T> {
    [JsonPropertyName("items")]
    public List<T> Items { get; set; }
}