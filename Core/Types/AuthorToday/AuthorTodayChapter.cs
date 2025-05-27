using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

    public string Decode(string userId, string cert) {
        var secret = $"{string.Concat(Key.Reverse())}:{(string.IsNullOrWhiteSpace(userId) ? "Guest" : userId)}:{"FjPg]{2+$8JRvv~("}:{cert}";
        var hashSecret = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(secret)));
        
        using var aes = Aes.Create();
        const int IV_SHIFT = 16;

        var aesKey = Encoding.UTF8.GetBytes(hashSecret)[..IV_SHIFT];
        
        aes.Key = aesKey; 
        aes.IV = aesKey;

        using var ms = new MemoryStream(Convert.FromBase64String(Text));
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        
        var output = new MemoryStream();
        cs.CopyTo(output);

        return Encoding.UTF8.GetString(output.ToArray());
    }
}