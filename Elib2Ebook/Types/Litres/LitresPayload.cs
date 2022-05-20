using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Elib2Ebook.Types.Litres; 

public class LitresPayload {
    [JsonPropertyName("sha")]
    public string Sha { get; set; }
    
    [JsonPropertyName("sid")]
    public string Sid { get; set; }

    [JsonPropertyName("uilang")] 
    public string UiLang { get; set; } = "rus";
    
    [JsonPropertyName("time")]
    public string Time { get; set; }
    
    [JsonPropertyName("mobile_app")]
    public string App { get; set; }

    [JsonPropertyName("requests")] 
    public List<object> Requests { get; set; } = new();

    public static LitresPayload Create(DateTime date, string sid, string secret, string app) {
        var result = new LitresPayload {
            Time = date.ToString("yyyy-MM-ddTHH:mm:sszzzz"),
            App = app,
            Sid = sid
        };
        
        result.Sha = Sha256(result.Time + secret);

        return result;
    }
    
    private static string Sha256(string str) {
        var crypt = SHA256.Create();
        var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(str), 0, Encoding.UTF8.GetByteCount(str));

        return crypto.Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString("x2"))).ToString();
    }
}