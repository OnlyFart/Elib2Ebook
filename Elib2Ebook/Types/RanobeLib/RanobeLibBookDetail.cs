using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.RanobeLib;

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

    [JsonPropertyName("slug_url")]
    public string SlugUrl { get; set; }

    [JsonPropertyName("cover")]
    public RLBDCover Cover { get; set; }

    [JsonPropertyName("authors")]
    public List<RLBDAuthor> Authors { get; set; }
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