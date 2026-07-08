using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.AuthorToday.Types;

internal class AuthResponse
{
    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("token")] public string Token { get; set; }
}
