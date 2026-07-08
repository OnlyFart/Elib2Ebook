using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.StrokiMts.Types;

internal class StrokiMtsContentInfo
{
    [JsonPropertyName("fileType")]
    public string FileType { get; set; }
}
