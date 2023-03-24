using System;
using System.Text.Json.Serialization;
using Elib2Ebook.Extensions;

namespace Elib2Ebook.Types.Wattpad; 

public class WattpadPart {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { private get; set; }

    public string GetTitle() {
        return Title.Replace("\r", " ").CollapseWhitespace().Trim();
    }

    public Uri Url => new($"https://www.wattpad.com/apiv2/storytext?id={Id}");
}