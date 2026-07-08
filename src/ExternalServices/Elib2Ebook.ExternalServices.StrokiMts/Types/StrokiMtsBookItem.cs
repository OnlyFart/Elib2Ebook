using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.StrokiMts.Types;

internal class StrokiMtsBookItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("annotation")]
    public string Annotation { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("imageUrl")]
    public Dictionary<string, string> ImageUrl { get; set; }

    [JsonPropertyName("authors")]
    public List<StrokiMtsAuthor> Authors { get; set; }

    [JsonPropertyName("contentInfo")]
    public StrokiMtsContentInfo ContentInfo { get; set; }
}
