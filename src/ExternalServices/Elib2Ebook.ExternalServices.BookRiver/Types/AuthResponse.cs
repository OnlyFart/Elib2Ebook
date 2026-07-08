using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.BookRiver.Types;

internal class AuthResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; }
}
