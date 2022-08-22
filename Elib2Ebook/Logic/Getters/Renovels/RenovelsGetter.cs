using System;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Renovels;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.Renovels; 

public class RenovelsGetter : RenovelsGetterBase {
    public RenovelsGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://renovels.org/");
    protected override string Segment => "novel";
    protected override HtmlDocument GetChapterAsHtml(RenovelsApiResponse<RenovelsChapter> response) {
        return response.Content.Content.AsHtmlDoc();
    }
}