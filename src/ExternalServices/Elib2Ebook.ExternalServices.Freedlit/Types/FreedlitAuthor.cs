using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Freedlit.Types;

internal class FreedlitAuthor
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("user_link")]
    public string UserLink { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
