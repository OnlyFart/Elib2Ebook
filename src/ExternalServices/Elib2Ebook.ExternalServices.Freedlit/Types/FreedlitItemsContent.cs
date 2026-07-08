using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Freedlit.Types;

internal class FreedlitItemsContent<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; }
}
