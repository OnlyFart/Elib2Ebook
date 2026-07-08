using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.LibSocial.Types.SocialLib;

internal class User
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
