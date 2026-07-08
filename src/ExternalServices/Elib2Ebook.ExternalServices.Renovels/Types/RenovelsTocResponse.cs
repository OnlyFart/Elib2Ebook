using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Renovels.Types;

internal class RenovelsTocResponse
{
    [JsonPropertyName("results")]
    public RenovelsChapter[] Results { get; set; }
}
