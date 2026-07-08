using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Litmarket.Types;

internal class AuthResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
