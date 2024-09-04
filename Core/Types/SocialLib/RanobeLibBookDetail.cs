using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Types.SocialLib;

public class RanobeLibBookDetails {
    [JsonPropertyName("data")]
    public RLBDData Data { get; set; }
}

public class RLBDData {
    public RLBDData(int? id) {
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
    public string Summary { get; set; }
}

public class RLBDCover {
    [JsonPropertyName("default")]
    public string Default { get; set; }
}

public class RLBDAuthor {
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slug_url")]
    public string SlugUrl { get; set; }
}