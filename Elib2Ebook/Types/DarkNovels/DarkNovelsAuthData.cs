using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.DarkNovels; 

public class DarkNovelsAuthResponse {
   [JsonPropertyName("token")]
   public DarkNovelsAuthToken Token { get; set; }
}