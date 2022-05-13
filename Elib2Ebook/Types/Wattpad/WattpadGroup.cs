using System;
using System.Text.Json.Serialization;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Types.Wattpad; 

public class WattpadGroup {
    [JsonPropertyName("ID")]
    public long Id { get; set; }
    
    [JsonPropertyName("TITLE")]
    public string Title { private get; set; }

    public string GetTitle() {
        return Title.Replace("\r", " ").CollapseWhitespace().Trim();
    }

    public Uri Url => new($"https://www.wattpad.com/apiv2/storytext?id={Id}");
}