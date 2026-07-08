using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Freedlit.Types;

internal class FreedlitAuthError
{
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("password")]
    public string[] Password { get; set; }
}
