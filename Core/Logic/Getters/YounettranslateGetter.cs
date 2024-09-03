using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Configs;
using Core.Extensions;
using Core.Types.Book;
using Core.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Core.Logic.Getters;

public class YounettranslateGetter : GetterBase {
    public YounettranslateGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://younettranslate.com/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/projects/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(url),
            Chapters = await FillChapters(url),
            Title = GetTitle(doc),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("p.short-p")?.InnerHtml
        };

        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        foreach (var div in doc.QuerySelectorAll("div.col-md-3")) {
            var label = div.QuerySelector("p.small-label");
            if (label != default  && label.GetText().StartsWith("Автор")) {
                return new Author(label.GetTextBySelector("+ p"));
            }
        }

        return new Author("Younettranslate");
    }

    private static string GetTitle(HtmlDocument doc) {
        return doc.GetTextBySelector("h1");
    }

    private async Task<IEnumerable<Chapter>> FillChapters(Uri url) {
        var result = new List<Chapter>();

        foreach (var urlChapter in await GetToc(url)) {
            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            var chapterDoc = await GetChapter(urlChapter);

            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, SystemUrl);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }

            result.Add(chapter);
        }

        return result;
    }

    private async Task<IEnumerable<UrlChapter>> GetToc(Uri url) {
        var result = new List<UrlChapter>();

        for (var i = 1;; i++) {
            var doc = await Config.Client.GetHtmlDocWithTriesAsync(url.AppendQueryParameter("page", i));
            var links = doc.QuerySelectorAll("table.catposts-table td.hidden-sm a[href]").Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText())).ToList();
            if (links.Count == 0) {
                break;
            }
            
            result.AddRange(links);
        }

        return SliceToc(result);
    }

    private async Task<HtmlDocument> GetChapter(UrlChapter urlChapter) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(urlChapter.Url);
        return doc.QuerySelector("div.postdata-content").RemoveNodes(n => n.InnerText.Contains("ЗАПРЕЩЕНО ПУБЛИКОВАТЬ")).InnerHtml.AsHtmlDoc();
    }

    private async Task<Image> GetCover(Uri bookUri) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync("https://younettranslate.com/projects/".AsUri());
        
        var imagePath = doc.QuerySelector($"div.category-item a[href='{bookUri}'] img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? await SaveImage(bookUri.MakeRelativeUri(imagePath)) : default;
    }
}