using System.Text.Json.Serialization;

namespace Core.Types.MyBook;

public class MyBookMapFile {
    [JsonPropertyName("book")]
    public long Book { get; set; }
}