using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Core.Types.AuthorToday; 

public class AuthorTodayChapter {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("sortOrder")]
    public long SortOrder { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("key")]
    public string Key { get; set; }
    
    [JsonPropertyName("IsSuccessful")]
    public bool IsSuccessful { get; set; }

    [JsonPropertyName("IsDraft")]
    public bool IsDraft { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; }

    public string Decode(string userId) {
        var secret = string.Join("", Key.Reverse()) + "@_@" + userId;
        var sb = new StringBuilder();
        for (var i = 0; i < Text.Length; i++) {
            sb.Append((char) (Text[i] ^ secret[i % secret.Length]));
        }

        return sb.ToString();
    }
}