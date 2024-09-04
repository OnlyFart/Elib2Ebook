using System;
using Core.Configs;
using Core.Extensions;
using Core.Types.Renovels;
using HtmlAgilityPack;

namespace Core.Logic.Getters.Renovels; 

public class RenovelsGetter : RenovelsGetterBase {
    public RenovelsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://renovels.org/");
    protected override string Segment => "novel";
    protected override HtmlDocument GetChapterAsHtml(RenovelsApiResponse<RenovelsChapter> response) {
        return response.Content.Content.AsHtmlDoc();
    }
}