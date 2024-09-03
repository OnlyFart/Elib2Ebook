using System.Text.Json.Serialization;

namespace Core.Types.DarkNovels; 

public class DarkNovelsAuthResponse {
   [JsonPropertyName("token")]
   public DarkNovelsAuthToken Token { get; set; }
}