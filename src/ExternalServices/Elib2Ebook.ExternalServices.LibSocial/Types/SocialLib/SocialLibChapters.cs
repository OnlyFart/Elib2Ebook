using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;

internal class SocialLibChapters
{
    [JsonPropertyName("list")]
    public List<SocialLibChapter> List { get; set; }
}
