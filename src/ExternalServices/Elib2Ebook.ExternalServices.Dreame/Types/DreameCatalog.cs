using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Dreame.Types;

internal class DreameCatalog
{
    [JsonPropertyName("pager")]
    public DreamePager Pager { get; set; }
}
