using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.StrokiMts;

public class StrokiMtsApiMultiResponse {
    [JsonPropertyName("items")]
    public List<StrokiMtsMultiItem> Items { get; set; }
}