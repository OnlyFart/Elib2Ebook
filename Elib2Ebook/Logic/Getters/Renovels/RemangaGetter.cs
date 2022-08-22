using System;
using System.Linq;
using System.Text;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Renovels;
using HtmlAgilityPack;

namespace Elib2Ebook.Logic.Getters.Renovels; 

public class RemangaGetter : RenovelsGetterBase {
    public RemangaGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://remanga.org/");
    protected override string Segment => "manga";
    protected override HtmlDocument GetChapterAsHtml(RenovelsApiResponse<RenovelsChapter> response) {
        var sb = new StringBuilder();

        foreach (var img in response.Content.Pages.SelectMany(p => p)) {
            sb.Append($"<img src='{img.Link}'/>");
        }

        return sb.AsHtmlDoc();
    }
}