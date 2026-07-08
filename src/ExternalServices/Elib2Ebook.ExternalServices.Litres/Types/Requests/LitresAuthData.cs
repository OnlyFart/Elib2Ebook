using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litres.Types.Requests;

internal class LitresAuthData
{
    [JsonPropertyName("login")]
    public string Login { get; set; }

    [JsonPropertyName("pwd")]
    public string Password { get; set; }
}
