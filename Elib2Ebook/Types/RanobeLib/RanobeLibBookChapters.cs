using System;
using System.Collections.Generic;
using System.Linq;
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

public class RanobeLibChapterBranch {
    [JsonPropertyName("branch_id")]
    public long? BranchId { get; set; }
}

public class RanobeLibChapterAttachment {
    [JsonPropertyName("url")]
    public string Url { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class RanobeLibBookChapter {
    [JsonPropertyName("id")]
    public long Id { get; set; } 
    
    [JsonPropertyName("manga_id")]
    public long MangaId { get; set; }
    
    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }
    
    [JsonPropertyName("branches")]
    public List<RanobeLibChapterBranch> Branches { get; set; }

    [JsonPropertyName("attachments")]
    public RanobeLibChapterAttachment[] Attachments { get; set; } = [];
 
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
    
    private static readonly Dictionary<string, string> RecursiveTag = new() {
        { "paragraph", "p" },
        { "orderedList", "ol" },
        { "listItem", "li" },
        { "blockquote", "blockquote" },
    };
    
    private static readonly Dictionary<string, string> InlineTag = new() {
        { "horizontalRule", "<hr />" },
        { "hardBreak", "<br />" },
    };
    
    private static readonly Dictionary<string, string> MarkTag = new() {
        { "italic", "i" },
        { "bold", "b" },
    };

    public HtmlDocument GetHtmlDoc() {
        switch (Content) {
            case JsonValue e:
                return e.GetValue<string>().AsHtmlDoc();
            case JsonObject o:
                var content = o.Deserialize<RanobeLibChapterContent>();
                return AsHtml(content.Content).AsHtmlDoc();
        }

        throw new Exception("Неизвестный тип");
    }

    private StringBuilder AsHtml(IEnumerable<RanobeLibChapterContent> contents) {
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

            switch (content.Type) {
                case "text": {
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
                case "image": {
                    if (content.Attrs.TryGetValue("images", out var images)) {
                        foreach (var image in images) {
                            if (!image.TryGetValue("image", out var imageId)) {
                                continue;
                            }
                            
                            var attachment = Attachments.FirstOrDefault(a => a.Name == imageId);
                            if (attachment == default) {
                                continue;
                            }
                            
                            sb.Append($"<img src=\"{attachment.Url}\" />");
                        }
                    }
                
                    continue;
                }
                default:
                    Console.WriteLine($"Неизвестый тип {content.Type}");
                    break;
            }
        }

        return sb;
    }
}

public class RanobeLibChapterContent {
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("marks")]
    public RanobeLibChapterMark[] Marks { get; set; } = [];

    [JsonPropertyName("content")]
    public RanobeLibChapterContent[] Content { get; set; } = [];

    [JsonPropertyName("attrs")]
    public Dictionary<string, Dictionary<string, string>[]> Attrs { get; set; } = new();
}

public class RanobeLibChapterMark {
    [JsonPropertyName("type")]
    public string Type { get; set; }
}