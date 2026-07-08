using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;

internal class WindowData
{
    [JsonPropertyName("chapters")]
    public SocialLibChapters Chapters { get; set; }

    [JsonPropertyName("user")]
    public User User { get; set; }
}
