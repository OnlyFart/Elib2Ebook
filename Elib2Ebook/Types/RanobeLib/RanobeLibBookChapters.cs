using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elib2Ebook.Extensions;
using HtmlAgilityPack;

namespace Elib2Ebook.Types.RanobeLib;

public class RanobeLibBookChapters {
    [JsonPropertyName("data")]
    public List<RanobeLibBookChapter> Chapters { get; set; }
}

public class RanobeLibBookChapterResponse {
    [JsonPropertyName("data")]
    public RanobeLibBookChapter Data { get; set; }
}

public class RanobeLibBookChapter {
    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    private string RawName { get; set; }

    [JsonPropertyName("name")]
    public string Name {
        get {
            var name = $"Том {Volume} Глава {Number}";
            if (!string.IsNullOrWhiteSpace(RawName)) {
                name += $" - {RawName}";
            }

            return name;
        }
        set { RawName = value; }
    }

    [JsonPropertyName("content")]
    public JsonNode Content { get; set; }

    public HtmlDocument GetHtmlDoc() {
        switch (Content) {
            case JsonValue e:
                return e.GetValue<string>().AsHtmlDoc();
            case JsonObject o:
                var content = o.Deserialize<RanobeLibChapterContent>();
                return content.AsHtmlDoc();
        }

        throw new Exception("Неизвестный тип");
    }
}

public class RanobeLibChapterContent {
    private static readonly Dictionary<string, string> RecursiveTag = new() {
        { "paragraph", "p" },
        { "orderedList", "ol" },
        { "listItem", "li" },
    };
    
    private static readonly Dictionary<string, string> InlineTag = new() {
        { "horizontalRule", "<hr />" },
        { "hardBreak", "<br />" },
    };
    
    private static readonly Dictionary<string, string> MarkTag = new() {
        { "italic", "i" },
        { "bold", "b" },
    };
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("marks")]
    public RanobeLibChapterMark[] Marks { get; set; } = [];

    [JsonPropertyName("content")]
    public RanobeLibChapterContent[] Content { get; set; } = [];

    public HtmlDocument AsHtmlDoc() {
        return AsHtml(Content).AsHtmlDoc();
    }

    private static StringBuilder AsHtml(RanobeLibChapterContent[] contents) {
        var sb = new StringBuilder();

        foreach (var content in contents) {
            if (RecursiveTag.TryGetValue(content.Type, out var tag)) {
                sb.Append(AsHtml(content.Content).ToString().CoverTag(tag)); 
                continue;
            }
            
            if (InlineTag.TryGetValue(content.Type, out tag)) {
                sb.Append(tag);
                continue;
            }

            if (content.Type == "text") {
                var text = content.Text;

                foreach (var mark in content.Marks) {
                    if (MarkTag.TryGetValue(mark.Type, out tag)) {
                        text = text.CoverTag(tag);
                    } else {
                        Console.WriteLine($"Неизвестый тип форматирования {mark.Type}");
                    }
                }

                sb.Append(text);
                continue;
            }
            
            Console.WriteLine($"Неизвестый тип {content.Type}");
        }

        return sb;
    }
}

public class RanobeLibChapterMark {
    [JsonPropertyName("type")]
    public string Type { get; set; }
}