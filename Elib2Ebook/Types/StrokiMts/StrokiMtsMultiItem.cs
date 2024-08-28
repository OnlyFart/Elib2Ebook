using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.StrokiMts;

public class StrokiMtsMultiItem {
    [JsonPropertyName("textBook")]
    public StrokiMtsBookItem TextBook { get; set; }
}