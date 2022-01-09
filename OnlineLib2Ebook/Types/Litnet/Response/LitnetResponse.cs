using System.Text.Json.Serialization;

namespace OnlineLib2Ebook.Types.Litnet.Response; 

public class LitnetResponse {
    [JsonPropertyName("status")]
    public int Status { get; set; }
        
    [JsonPropertyName("data")]
    public string Data { get; set; }
        
    [JsonPropertyName("isLastPage")]
    public bool IsLastPage { get; set; }
}