using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Configs;
using Core.Extensions;
using Core.Types.Renovels;
using HtmlAgilityPack;

namespace Core.Logic.Getters.Renovels; 

public class RemangaGetter : RenovelsGetterBase {
    public RemangaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://remanga.org/");
    protected override string Segment => "manga";
    protected override HtmlDocument GetChapterAsHtml(RenovelsApiResponse<RenovelsChapter> response) {
        var sb = new StringBuilder();

        foreach (var obj in response.Content.Pages) {
            switch (obj) {
                case JsonObject:
                    sb.Append($"<img src='{obj.Deserialize<RenovelsPage>().Link}'/>");
                    break;
                case JsonArray pages: {
                    foreach (var page in pages) {
                        sb.Append($"<img src='{page.Deserialize<RenovelsPage>().Link}'/>");
                    }

                    break;
                }
            }
        }

        return sb.AsHtmlDoc();
    }
}