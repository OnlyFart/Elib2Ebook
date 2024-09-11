using System;
using System.Text.Json.Serialization;
using Core.Extensions;

namespace Core.Types.Wattpad; 

public class WattpadPart {
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { private get; set; }

    public string FullName => Title.Replace("\r", " ").CollapseWhitespace().Trim();

    public Uri Url => new($"https://www.wattpad.com/apiv2/storytext?id={Id}");
}