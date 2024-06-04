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
            switch (content.Type) {
                case "paragraph":
                    sb.Append(AsHtml(content.Content).ToString().CoverTag("p"));
                    break;
                case "orderedList":
                    sb.Append(AsHtml(content.Content).ToString().CoverTag("ol"));
                    break;
                case "listItem":
                    sb.Append(AsHtml(content.Content).ToString().CoverTag("li"));
                    break;
                case "horizontalRule":
                    sb.Append("<hr />");
                    break;
                case "hardBreak":
                    sb.Append("<br />");
                    break;
                case "text": {
                    var text = content.Text;

                    foreach (var mark in content.Marks) {
                        switch (mark.Type) {
                            case "italic":
                                text = text.CoverTag("i");
                                break;
                            case "bold":
                                text = text.CoverTag("b");
                                break;
                            default:
                                Console.WriteLine($"Неизвестый тип форматирования {mark.Type}");
                                break;
                        }
                    }

                    sb.Append(text);
                    break;
                }
                default:
                    Console.WriteLine($"Неизвестый тип {content.Type}");
                    break;
            }
        }

        return sb;
    }
}

public class RanobeLibChapterMark {
    [JsonPropertyName("type")]
    public string Type { get; set; }
}