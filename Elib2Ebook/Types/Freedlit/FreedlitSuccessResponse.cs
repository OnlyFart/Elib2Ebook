using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Freedlit;

public class FreedlitSuccessResponse<T> {
    [JsonPropertyName("items")]
    public List<T> Items { get; set; }
}