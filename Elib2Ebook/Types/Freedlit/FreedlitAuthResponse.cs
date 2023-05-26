using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Freedlit; 

public class FreedlitAuthResponse {
    [JsonPropertyName("errors")]
    public FreedlitAuthError Errors { get; set; }
}