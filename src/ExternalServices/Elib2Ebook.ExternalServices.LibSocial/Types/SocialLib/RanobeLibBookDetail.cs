using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;

internal class RanobeLibBookDetails
{
    [JsonPropertyName("data")]
    public RLBDData Data { get; set; }
}

internal class RLBDData
{
    public RLBDData(int? id)
    {
        Id = id;
    }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rus_name")]
    public string RusName { get; set; }

    [JsonPropertyName("slug_url")]
    public string SlugUrl { get; set; }

    [JsonPropertyName("cover")]
    public RLBDCover Cover { get; set; }

    [JsonPropertyName("authors")]
    public List<RLBDAuthor> Authors { get; set; }

    [JsonPropertyName("summary")]
    public JsonNode Summary { get; set; }
}

internal class RLBDCover
{
    [JsonPropertyName("default")]
    public string Default { get; set; }
}

internal class RLBDAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slug_url")]
    public string SlugUrl { get; set; }
}
