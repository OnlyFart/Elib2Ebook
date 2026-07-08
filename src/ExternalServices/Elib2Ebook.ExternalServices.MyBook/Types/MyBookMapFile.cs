using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.MyBook.Types;

internal class MyBookMapFile
{
    [JsonPropertyName("book")]
    public long Book { get; set; }
}
