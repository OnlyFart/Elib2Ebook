using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.StrokiMts;

public class StrokiMtsApiMultiResponse {
    [JsonPropertyName("items")]
    public List<StrokiMtsMultiItem> Items { get; set; }
}