using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Requests;

internal class LitresAuthData
{
    [JsonPropertyName("login")]
    public string Login { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }
}
