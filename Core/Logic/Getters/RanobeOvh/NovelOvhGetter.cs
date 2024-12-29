using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.RanobeOvh;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Core.Logic.Getters.RanobeOvh; 

public class NovelOvhGetter(BookGetterConfig config) : RanobeOvhGetterBase(config) {
    protected override Uri SystemUrl => new("https://novel.ovh/");
    
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
        { "underline", "u" },
    };

    private StringBuilder AsHtml(RanobeOvhChapterFull chapterFullResponse, IEnumerable<RanobeOvhChapterContent> contents) {
        var sb = new StringBuilder();

        foreach (var content in contents) {
            if (RecursiveTag.TryGetValue(content.Type, out var tag)) {
                sb.Append(AsHtml(chapterFullResponse, content.Content).ToString().CoverTag(tag)); 
                continue;
            }
            
            if (InlineTag.TryGetValue(content.Type, out tag)) {
                sb.Append(tag);
                continue;
            }

            switch (content.Type) {
                case "text": {
                    var text = content.Text.HtmlEncode();

                    foreach (var mark in content.Marks) {
                        if (MarkTag.TryGetValue(mark.Type, out tag)) {
                            text = text.CoverTag(tag);
                        } else {
                            Config.Logger.LogInformation($"Неизвестый тип форматирования {mark.Type}");
                        }
                    }

                    sb.Append(text);
                    continue;
                }
                case "image": {
                    if (content.Attrs.TryGetValue("pages", out var pages)) {
                        foreach (var imageId in pages.Deserialize<string[]>()) {
                            var attachment = chapterFullResponse.Pages.FirstOrDefault(a => a.Id == imageId);
                            if (attachment == default) {
                                continue;
                            }
                            
                            sb.Append($"<img src=\"{attachment.Image}\" />");
                        }
                    }
                
                    continue;
                }
                default:
                    Config.Logger.LogInformation($"Неизвестый тип {content.Type}");
                    break;
            }
        }

        return sb;
    }
    
    protected override async Task<HtmlDocument> GetChapter(RanobeOvhChapterShort ranobeOvhChapterFull) {
        var data = await Config.Client.GetFromJsonAsync<RanobeOvhChapterFull>($"https://api.{SystemUrl.Host}/v2/chapters/{ranobeOvhChapterFull.Id}");
        return data.Content switch {
            JsonValue e => e.GetValue<string>().AsHtmlDoc(),
            JsonObject o => AsHtml(data, o.Deserialize<RanobeOvhChapterContent>().Content).AsHtmlDoc(),
            _ => throw new Exception("Неизвестный тип")
        };
    }
}