using System.Text.Json.Serialization;

namespace Elib2Ebook.ExternalServices.Freedlit.Types;

internal class FreedlitApiResponse<T>
{
    [JsonPropertyName("success")]
    public T Success { get; set; }
}
