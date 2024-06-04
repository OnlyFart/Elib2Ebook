using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.StrokiMts;

public class StrokiMtsFile {
    [JsonPropertyName("fileId")]
    public long FileId { get; set; }
}