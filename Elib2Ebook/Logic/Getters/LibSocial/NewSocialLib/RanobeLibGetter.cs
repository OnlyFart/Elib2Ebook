using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.SocialLib;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.LibSocial.NewSocialLib; 

public class RanobeLibGetter : NewLibSocialGetterBase {
    public RanobeLibGetter(BookGetterConfig config) : base(config) { }
    
    protected override Uri SystemUrl => new("https://ranobelib.me/");
    
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

    private StringBuilder AsHtml(SocialLibBookChapter chapterResponse, IEnumerable<SocialLibChapterContent> contents) {
        var sb = new StringBuilder();

        foreach (var content in contents) {
            if (RecursiveTag.TryGetValue(content.Type, out var tag)) {
                sb.Append(AsHtml(chapterResponse, content.Content).ToString().CoverTag(tag)); 
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
                        foreach (var image in images.Deserialize<Dictionary<string, string>[]>()) {
                            if (!image.TryGetValue("image", out var imageId)) {
                                continue;
                            }
                            
                            var attachment = chapterResponse.Attachments.FirstOrDefault(a => a.Name == imageId);
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
    
    protected override HtmlDocument ResponseToHtmlDoc(SocialLibBookChapter chapterResponse) {
        return chapterResponse.Content switch {
            JsonValue e => e.GetValue<string>().AsHtmlDoc(),
            JsonObject o => AsHtml(chapterResponse, o.Deserialize<SocialLibChapterContent>().Content).AsHtmlDoc(),
            _ => throw new Exception("Неизвестный тип")
        };
    }
}