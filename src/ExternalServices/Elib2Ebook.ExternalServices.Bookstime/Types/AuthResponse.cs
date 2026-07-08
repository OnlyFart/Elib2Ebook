using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Bookstime.Types;

internal class AuthResponse
{
    [JsonPropertyName("X_OCTOBER_ERROR_MESSAGE")]
    public string XOctoberErrorMessage { get; set; }
}
