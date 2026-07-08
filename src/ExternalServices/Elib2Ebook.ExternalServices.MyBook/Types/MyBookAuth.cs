using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.MyBook.Types;

internal class MyBookAuth
{
    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("secret")]
    public string Secret { get; set; }
}
