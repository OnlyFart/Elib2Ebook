using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.StrokiMts.Types;

internal class StrokiMtsMultiItem
{
    [JsonPropertyName("textBook")]
    public StrokiMtsBookItem TextBook { get; set; }

    [JsonPropertyName("audioBook")]
    public StrokiMtsBookItem AudioBook { get; set; }

    [JsonPropertyName("comics")]
    public StrokiMtsBookItem ComicBook { get; set; }

    [JsonPropertyName("pressa")]
    public StrokiMtsBookItem PressBook { get; set; }
}
