using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OnlineLib2Ebook.Types.Wattpad; 

public class WattpadGroup {
    [JsonPropertyName("ID")]
    public long Id { get; set; }
    
    [JsonPropertyName("TITLE")]
    public string Title { private get; set; }

    public string GetTitle() {
        return Regex.Replace(Title.Replace("\r", " "), "\\s+", " ").Trim();
    }

    public Uri Url => new($"https://www.wattpad.com/apiv2/storytext?id={Id}");
}