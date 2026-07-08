using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Response;

internal class LitresFiles
{
    [JsonPropertyName("files")]
    public LitresFile[] Files { get; set; }
}
